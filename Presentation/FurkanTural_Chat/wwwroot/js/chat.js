(function () {
    'use strict';

    const cfg = window.CHAT;
    if (!cfg) return;

    const API = cfg.apiBase;
    const me = cfg.userId;

    const $ = (id) => document.getElementById(id);
    const friendsList = $('friendsList');
    const requestsList = $('requestsList');
    const requestBadge = $('requestBadge');
    const requestsPanel = $('requestsPanel');
    const searchInput = $('searchInput');
    const searchResults = $('searchResults');
    const messagesEl = $('messages');
    const composer = $('composer');
    const messageInput = $('messageInput');
    const convTitle = $('convTitle');
    const convPresence = $('convPresence');
    const convAvatar = $('convAvatar');
    const typingIndicator = $('typingIndicator');
    const connStatus = $('connStatus');
    const attachMenuBtn = $('attachMenuBtn');
    const attachMenu = $('attachMenu');
    const recTimer = $('recTimer');
    const chatApp = document.querySelector('.chat-app');   // mobil: liste ↔ sohbet geçişi

    let currentFriend = null;          // { id, name }
    const friends = new Map();         // friendUserId -> friend dto
    const unread = new Map();          // friendUserId -> count
    let searchTimer = null;
    let typingThrottle = null;
    let typingClearTimer = null;

    const INITIAL_TAKE = 60;           // konuşma açılışında yüklenen mesaj sayısı
    const convTake = new Map();        // friendUserId -> o sohbet için geçerli take
    const convSummaries = new Map();   // friendUserId -> { text, type, at } (kenar çubuğu önizlemesi)
    const baseTitle = document.title;  // okunmamış rozeti için temel sekme başlığı
    let lastDayKey = null;             // mesaj listesinde son çizilen günün anahtarı (tarih ayracı)

    function esc(s) { const d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }
    function initial(name) { return ((name || '?').trim().charAt(0) || '?').toUpperCase(); }
    function fmtTime(iso) { return window.FtTime ? FtTime.time(iso) : (function () { try { return new Date(iso).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul' }); } catch (e) { return ''; } })(); }
    function toast(msg, type) { if (window.showToast) window.showToast(type === 'error' ? 'error' : 'info', type === 'error' ? 'Hata' : 'Bilgi', msg); }
    // Avatarlar (users/images/..) API'den statik sunulur: apiBase + '/' + value; eski kayıtlar düz dosya adı → images/uploads.
    function mediaUrl(value) {
        if (!value) return '';
        return value.indexOf('/') >= 0 ? API + '/' + value : API + '/images/uploads/' + value;
    }
    // Sohbet ekleri (ses/foto/video) gizlidir; statik sunulmaz. BFF üzerinden yetkili uçtan akar
    // (oturum cookie'si ile same-origin istek → YARP JWT ekler → API katılımcı doğrular).
    function attachmentSrc(value) {
        if (!value) return '';
        return '/bff/api/v1/message/attachment?file=' + encodeURIComponent(value);
    }
    function avatarMarkup(name, avatarUrl, cls) {
        cls = cls || 'avatar sm';
        if (avatarUrl) {
            var url = mediaUrl(avatarUrl);
            return '<div class="' + cls + '" style="background-image:url(\'' + url + '\');background-size:cover;background-position:center;"></div>';
        }
        return '<div class="' + cls + '">' + esc(initial(name)) + '</div>';
    }

    // "son görülme" için göreli Türkçe zaman (ortak FtTime üzerinden).
    function relTime(iso) {
        if (!iso) return '';
        if (window.FtTime) return FtTime.relative(iso);
        var d = new Date(iso), now = new Date();
        var s = Math.floor((now - d) / 1000);
        if (isNaN(s)) return '';
        if (s < 60) return 'az önce';
        var m = Math.floor(s / 60); if (m < 60) return m + ' dk önce';
        var h = Math.floor(m / 60); if (h < 24) return h + ' sa önce';
        var days = Math.floor(h / 24);
        if (days === 1) return 'dün';
        if (days < 7) return days + ' gün önce';
        try { return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', timeZone: 'Europe/Istanbul' }); } catch (e) { return ''; }
    }
    // Arkadaşın aktiflik metni (çevrimiçi / son görülme / çevrimdışı).
    function presenceText(f) {
        if (!f) return '';
        if (f.isOnline) return 'çevrimiçi';
        if (f.lastSeenAt) return 'son görülme ' + relTime(f.lastSeenAt);
        return 'çevrimdışı';
    }

    // ── tarih ayraçları (Bugün / Dün / 13 Haziran 2026) — Istanbul gününe göre ──
    function dayKey(iso) {
        if (!iso) return '';
        if (window.FtTime) return FtTime.dateInput(iso);
        try { return new Date(iso).toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' }); } catch (e) { return ''; }
    }
    function dayLabel(iso) {
        const key = dayKey(iso);
        if (!key) return '';
        const now = Date.now();
        if (key === dayKey(new Date(now).toISOString())) return 'Bugün';
        if (key === dayKey(new Date(now - 86400000).toISOString())) return 'Dün';
        return window.FtTime ? FtTime.date(iso) : key;
    }
    function daySepEl(iso) {
        const div = document.createElement('div');
        div.className = 'day-sep';
        div.innerHTML = '<span>' + esc(dayLabel(iso)) + '</span>';
        return div;
    }

    // BFF: same-origin '/bff/*' proxy'sine gider; JWT'yi sunucu (YARP) ekler, tarayıcı sadece
    // HttpOnly session cookie'siyle kimliklenir (Authorization header'ı burada YOK).
    async function api(path, opts) {
        opts = opts || {};
        opts.headers = opts.headers || {};
        if (opts.body && typeof opts.body !== 'string') {
            opts.headers['Content-Type'] = 'application/json';
            opts.body = JSON.stringify(opts.body);
        }
        let res;
        try { res = await fetch('/bff' + path, opts); }
        catch (e) { return null; }
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        try { return await res.json(); } catch (e) { return null; }
    }

    function errOf(r, fallback) { return (r && r.errors && r.errors[0]) || (r && r.message) || fallback; }

    async function loadFriends() {
        const r = await api('/api/v1/friend');
        if (r && r.success) renderFriends(r.data || []);
    }

    function renderFriends(list) {
        friends.clear();
        friendsList.innerHTML = '';
        if (!list.length) { friendsList.innerHTML = '<div class="empty-hint small">Henüz arkadaşın yok. Yukarıdan ara ve ekle.</div>'; return; }
        list.forEach(f => {
            friends.set(f.friendUserId, f);
            const name = f.displayName || f.username;
            const div = document.createElement('div');
            div.className = 'friend-item';
            div.dataset.id = f.friendUserId;
            div.innerHTML =
                '<div class="avatar-wrap">' + avatarMarkup(name, f.avatarUrl) +
                    '<span class="status-dot' + (f.isOnline ? ' online' : '') + '"></span></div>' +
                '<div class="friend-meta"><div class="friend-name">' + esc(name) + '</div>' +
                '<div class="friend-sub">@' + esc(f.username) + '</div>' +
                '<div class="friend-status' + (f.isOnline ? ' online' : '') + '">' + esc(presenceText(f)) + '</div></div>' +
                '<span class="unread-dot" hidden></span>';
            // Avatara tıklayınca profil aç (sohbet açılmaz); satıra tıklayınca sohbet aç.
            const avWrap = div.querySelector('.avatar-wrap');
            if (avWrap) avWrap.addEventListener('click', (e) => { e.stopPropagation(); if (window.Profile) window.Profile.open(f.friendUserId); });
            div.addEventListener('click', () => openConversation(f.friendUserId));
            friendsList.appendChild(div);
        });
        refreshUnreadDots();
        updateFriendPreviews();
    }

    // Okunmamış sayılarını ve son mesaj önizlemelerini sunucudan tazele.
    // Sunucu anlık görüntüsü yetkilidir: silme/düzenleme sonrası eski yerel değer kalmasın diye temizlenir.
    async function loadUnread() {
        const r = await api('/api/v1/message/conversations');
        if (!(r && r.success)) return;
        unread.clear();
        convSummaries.clear();
        (r.data || []).forEach(c => {
            if (c.unreadCount > 0) unread.set(c.friendUserId, c.unreadCount);
            if (c.lastMessageAt) convSummaries.set(c.friendUserId, { text: c.lastMessage, type: c.lastMessageType, at: c.lastMessageAt });
        });
        refreshUnreadDots();
        updateFriendPreviews();
    }

    function previewText(s) {
        const t = (s.type || 'text').toLowerCase();
        if (t === 'audio') return '🎤 Sesli mesaj';
        if (t === 'image') return '📷 Fotoğraf';
        if (t === 'video') return '🎬 Video';
        return s.text || '';
    }

    // Konuşması olan arkadaşta aktiflik satırı yerine son mesaj önizlemesi gösterilir
    // (aktiflik avatardaki yeşil nokta + sohbet başlığında zaten görünür).
    function updateFriendPreviews() {
        document.querySelectorAll('.friend-item').forEach(el => {
            const id = +el.dataset.id;
            const st = el.querySelector('.friend-status');
            if (!st) return;
            const s = convSummaries.get(id);
            if (s) {
                st.textContent = previewText(s);
                st.classList.add('is-preview');
                st.classList.remove('online');
            } else {
                const f = friends.get(id);
                st.textContent = presenceText(f);
                st.classList.toggle('online', !!(f && f.isOnline));
                st.classList.remove('is-preview');
            }
        });
        sortFriendList();
    }

    function sortFriendList() {
        const items = Array.from(friendsList.querySelectorAll('.friend-item'));
        items.sort((a, b) => {
            const sa = convSummaries.get(+a.dataset.id), sb = convSummaries.get(+b.dataset.id);
            const ta = sa ? (Date.parse(sa.at) || 0) : 0, tb = sb ? (Date.parse(sb.at) || 0) : 0;
            if (ta !== tb) return tb - ta;   // en son konuşulan en üstte
            const na = a.querySelector('.friend-name'), nb = b.querySelector('.friend-name');
            return (na ? na.textContent : '').localeCompare(nb ? nb.textContent : '', 'tr');
        });
        items.forEach(el => friendsList.appendChild(el));
    }

    // Yeni mesajda (gelen/giden) önizlemeyi ve sırayı güncelle.
    function noteSummary(friendId, m) {
        if (!friendId || !m) return;
        convSummaries.set(friendId, { text: m.content, type: m.messageType, at: m.createdAt });
        updateFriendPreviews();
    }

    searchInput.addEventListener('input', () => {
        clearTimeout(searchTimer);
        const q = searchInput.value.trim();
        if (q.length < 2) { searchResults.hidden = true; searchResults.innerHTML = ''; return; }
        searchTimer = setTimeout(() => doSearch(q), 300);
    });

    async function doSearch(q) {
        const r = await api('/api/v1/user/search?query=' + encodeURIComponent(q));
        if (!(r && r.success)) { searchResults.hidden = true; return; }
        const list = r.data || [];
        searchResults.innerHTML = '';
        if (!list.length) { searchResults.innerHTML = '<div class="empty-hint small">Sonuç bulunamadı.</div>'; searchResults.hidden = false; return; }
        list.forEach(u => {
            const name = u.displayName || u.username;
            const div = document.createElement('div');
            div.className = 'search-item';
            div.innerHTML =
                avatarMarkup(name, u.avatarUrl) +
                '<div class="friend-meta"><div class="friend-name">' + esc(name) + '</div>' +
                '<div class="friend-sub">@' + esc(u.username) + '</div></div>' +
                '<button class="btn-add">Ekle</button>';
            div.querySelector('.btn-add').addEventListener('click', async (e) => {
                const btn = e.currentTarget;
                btn.disabled = true; btn.textContent = '…';
                const res = await api('/api/v1/friend/request', { method: 'POST', body: { addresseeId: u.id } });
                if (res && res.success) { btn.textContent = 'Gönderildi'; loadRequests(); }
                else { btn.disabled = false; btn.textContent = 'Ekle'; toast(errOf(res, 'İstek gönderilemedi.'), 'error'); }
            });
            searchResults.appendChild(div);
        });
        searchResults.hidden = false;
    }

    document.addEventListener('click', (e) => {
        if (!searchResults.contains(e.target) && e.target !== searchInput) searchResults.hidden = true;
    });

    async function loadRequests() {
        const [inc, out] = await Promise.all([
            api('/api/v1/friend/requests'),
            api('/api/v1/friend/requests/sent')
        ]);
        const incoming = (inc && inc.success) ? (inc.data || []) : [];
        const outgoing = (out && out.success) ? (out.data || []) : [];
        renderRequests(incoming, outgoing);
    }

    function renderRequests(incoming, outgoing) {
        incoming = incoming || []; outgoing = outgoing || [];
        requestsList.innerHTML = '';
        updateBadge(incoming.length);   // rozet yalnızca GELEN (aksiyon alınabilir) istekleri sayar
        if (!incoming.length && !outgoing.length) {
            requestsList.innerHTML = '<div class="empty-hint small">Bekleyen istek yok.</div>';
            return;
        }
        // Gelen istekler — kabul / reddet
        incoming.forEach(req => {
            const name = req.displayName || req.username;
            const div = document.createElement('div');
            div.className = 'request-item';
            div.innerHTML =
                avatarMarkup(name, req.avatarUrl) +
                '<div class="friend-meta"><div class="friend-name">' + esc(name) + '</div>' +
                '<div class="friend-sub">@' + esc(req.username) + '</div></div>' +
                '<div class="req-actions"><button class="btn-accept" title="Kabul et">✓</button>' +
                '<button class="btn-reject" title="Reddet">✕</button></div>';
            div.querySelector('.btn-accept').addEventListener('click', () => respondRequest(req.requestId, true));
            div.querySelector('.btn-reject').addEventListener('click', () => respondRequest(req.requestId, false));
            requestsList.appendChild(div);
        });
        // Giden istekler — salt-okunur "Bekliyor" (statü değiştirilemez)
        outgoing.forEach(req => {
            const name = req.displayName || req.username;
            const div = document.createElement('div');
            div.className = 'request-item request-item--sent';
            div.innerHTML =
                avatarMarkup(name, req.avatarUrl) +
                '<div class="friend-meta"><div class="friend-name">' + esc(name) + '</div>' +
                '<div class="friend-sub">@' + esc(req.username) + '</div></div>' +
                '<span class="req-pending" title="Onay bekliyor">Bekliyor</span>';
            requestsList.appendChild(div);
        });
    }

    function updateBadge(n) {
        if (n > 0) { requestBadge.textContent = n; requestBadge.hidden = false; requestsPanel.classList.add('has-requests'); }
        else { requestBadge.hidden = true; requestBadge.textContent = ''; requestsPanel.classList.remove('has-requests'); }
    }

    async function respondRequest(id, accept) {
        const res = await api('/api/v1/friend/' + id + '/' + (accept ? 'accept' : 'reject'), { method: 'POST' });
        if (res && res.success) {
            await loadRequests();          // rozet anında güncellenir
            if (accept) await loadFriends();
        } else {
            toast(errOf(res, 'İşlem başarısız.'), 'error');
        }
    }

    function flashRequests() {
        requestsPanel.classList.add('flash');
        setTimeout(() => requestsPanel.classList.remove('flash'), 1800);
    }

    async function openConversation(friendId) {
        const f = friends.get(friendId);
        if (!f) return;
        currentFriend = { id: friendId, name: f.displayName || f.username };
        document.querySelectorAll('.friend-item').forEach(el => el.classList.toggle('active', +el.dataset.id === friendId));
        convTitle.textContent = currentFriend.name;
        updateConvPresence(f);
        setConvAvatar(f);
        if (chatApp) chatApp.classList.add('show-conversation');   // mobil: sohbet panelini öne getir
        typingIndicator.hidden = true;
        composer.hidden = false;
        messagesEl.innerHTML = '<div class="empty-hint">Yükleniyor…</div>';

        // Tüm geçmiş yerine son N mesaj yüklenir; "daha eski" butonu kapsamı genişletir.
        const take = convTake.get(friendId) || INITIAL_TAKE;
        const r = await api('/api/v1/message/conversation/' + friendId + '?take=' + take);
        if (r && r.success) renderMessages(r.data || [], friendId, take, true);
        else messagesEl.innerHTML = '<div class="empty-hint">Mesajlar yüklenemedi.</div>';

        unread.set(friendId, 0);
        refreshUnreadDots();
        api('/api/v1/message/' + friendId + '/read', { method: 'POST' }); // okundu -> gönderene bildirilir
        messageInput.focus();
        ['callAudioBtn', 'callVideoBtn', 'callMenuBtn', 'convMenuBtn'].forEach(function (cid) { var b = $(cid); if (b) b.hidden = false; });
        document.dispatchEvent(new CustomEvent('chat:conversationchanged', { detail: { friendId: friendId } }));
    }

    function renderMessages(list, friendId, take, scrollBottom) {
        messagesEl.innerHTML = '';
        lastDayKey = null;
        if (!list.length) { messagesEl.innerHTML = '<div class="empty-hint">Henüz mesaj yok. İlk mesajı sen gönder!</div>'; return; }
        // Sunucu son `take` mesajı döndürür; liste dolu geldiyse daha eskisi olabilir.
        if (friendId && take && list.length >= take) {
            const older = document.createElement('button');
            older.type = 'button';
            older.className = 'load-older';
            older.textContent = 'Daha eski mesajları göster';
            older.addEventListener('click', async () => {
                older.disabled = true; older.textContent = 'Yükleniyor…';
                const newTake = (convTake.get(friendId) || INITIAL_TAKE) * 3;
                convTake.set(friendId, newTake);
                const r = await api('/api/v1/message/conversation/' + friendId + '?take=' + newTake);
                if (r && r.success) renderMessages(r.data || [], friendId, newTake, false); // konum: en üstte kal
                else { older.disabled = false; older.textContent = 'Daha eski mesajları göster'; }
            });
            messagesEl.appendChild(older);
        }
        list.forEach(m => appendWithDaySep(m));
        if (scrollBottom !== false) scrollToBottom();
    }

    // Gün değiştiyse önce tarih ayracı, sonra mesajı ekler. ("Henüz mesaj yok" ipucunu temizler.)
    function appendWithDaySep(m) {
        const hint = messagesEl.querySelector('.empty-hint');
        if (hint) { hint.remove(); lastDayKey = null; }
        const key = dayKey(m.createdAt);
        if (key && key !== lastDayKey) {
            messagesEl.appendChild(daySepEl(m.createdAt));
            lastDayKey = key;
        }
        messagesEl.appendChild(messageEl(m));
    }

    function messageEl(m) {
        const out = m.senderId === me;
        const div = document.createElement('div');
        div.className = 'msg ' + (out ? 'out' : 'in');
        div.dataset.id = m.id;
        div.dataset.type = (m.messageType || 'text').toLowerCase();
        div.dataset.created = m.createdAt || '';

        const type = (m.messageType || 'text').toLowerCase();
        let bubble;
        if (type === 'audio' && m.attachmentUrl) {
            bubble = '<div class="bubble audio"><audio controls preload="metadata" src="' + attachmentSrc(m.attachmentUrl) + '"></audio></div>';
        } else if (type === 'image' && m.attachmentUrl) {
            const iurl = attachmentSrc(m.attachmentUrl);
            bubble = '<div class="bubble media"><img class="msg-img" src="' + iurl + '" alt="Görsel" loading="lazy" data-full="' + iurl + '"></div>';
        } else if (type === 'video' && m.attachmentUrl) {
            bubble = '<div class="bubble media"><video class="msg-video" controls preload="metadata" src="' + attachmentSrc(m.attachmentUrl) + '"></video></div>';
        } else {
            bubble = '<div class="bubble">' + esc(m.content) + '</div>';
        }

        let meta = '';
        if (m.editedAt) meta += '<span class="edited">düzenlendi</span> ';
        meta += '<span>' + esc(fmtTime(m.createdAt)) + '</span>';
        if (out) meta += ' <span class="ticks' + (m.isRead ? ' read' : '') + '">' + (m.isRead ? '✓✓' : '✓') + '</span>';

        div.innerHTML = bubble + '<div class="msg-meta">' + meta + '</div>';

        // Kendi mesajında işlem menüsü (düzenle / sil).
        if (out) {
            const act = document.createElement('button');
            act.type = 'button';
            act.className = 'msg-act';
            act.title = 'Mesaj işlemleri';
            act.setAttribute('aria-label', 'Mesaj işlemleri');
            act.textContent = '⋯';
            act.addEventListener('click', (e) => { e.stopPropagation(); openMsgMenu(div, act); });
            div.appendChild(act);
        }
        return div;
    }

    const EDIT_WINDOW_MS = 15 * 60 * 1000;   // sunucudaki düzenleme penceresiyle aynı
    let msgMenu = null, msgMenuTarget = null;

    function ensureMsgMenu() {
        if (msgMenu) return msgMenu;
        msgMenu = document.createElement('div');
        msgMenu.className = 'conv-menu msg-menu';
        msgMenu.hidden = true;
        msgMenu.innerHTML =
            '<button type="button" data-msg-act="edit"><span>Düzenle</span></button>' +
            '<button type="button" data-msg-act="delete" class="danger"><span>Sil</span></button>';
        document.body.appendChild(msgMenu);
        msgMenu.addEventListener('click', (e) => e.stopPropagation());
        document.addEventListener('click', () => { msgMenu.hidden = true; });
        msgMenu.querySelector('[data-msg-act="edit"]').addEventListener('click', () => { msgMenu.hidden = true; editTargetMessage(); });
        msgMenu.querySelector('[data-msg-act="delete"]').addEventListener('click', () => { msgMenu.hidden = true; deleteTargetMessage(); });
        return msgMenu;
    }

    function openMsgMenu(div, anchor) {
        const menu = ensureMsgMenu();
        msgMenuTarget = div;
        // Düzenle: yalnızca metin mesajı ve düzenleme penceresi içindeyse.
        const isText = (div.dataset.type || 'text') === 'text';
        const created = Date.parse(div.dataset.created || '') || 0;
        menu.querySelector('[data-msg-act="edit"]').hidden = !(isText && (Date.now() - created) <= EDIT_WINDOW_MS);
        menu.hidden = false;
        const r = anchor.getBoundingClientRect();
        const mw = menu.offsetWidth || 140, mh = menu.offsetHeight || 80;
        menu.style.left = Math.max(8, Math.min(r.left, window.innerWidth - mw - 8)) + 'px';
        menu.style.top = (r.bottom + mh > window.innerHeight - 8 ? r.top - mh - 4 : r.bottom + 4) + 'px';
    }

    async function deleteTargetMessage() {
        const div = msgMenuTarget;
        if (!div) return;
        if (!window.confirm('Bu mesaj silinsin mi? Her iki taraftan da kaldırılır.')) return;
        const r = await api('/api/v1/message/' + div.dataset.id, { method: 'DELETE' });
        if (r && r.success) removeMessageEl(+div.dataset.id);   // sayaç/önizleme MessageDeleted olayıyla tazelenir
        else toast(errOf(r, 'Mesaj silinemedi.'), 'error');
    }

    async function editTargetMessage() {
        const div = msgMenuTarget;
        if (!div) return;
        const bubble = div.querySelector('.bubble');
        const current = bubble ? bubble.textContent : '';
        const text = window.prompt('Mesajı düzenle:', current);
        if (text === null) return;   // iptal
        const trimmed = text.trim();
        if (!trimmed || trimmed === current) return;
        const r = await api('/api/v1/message/' + div.dataset.id, { method: 'PUT', body: { content: trimmed } });
        if (r && r.success && r.data) applyEditedMessage(r.data);
        else toast(errOf(r, 'Mesaj düzenlenemedi.'), 'error');
    }

    function removeMessageEl(id) {
        const el = messagesEl.querySelector('.msg[data-id="' + id + '"]');
        if (!el) return;
        // Günün tek mesajıysa öksüz tarih ayracı bırakma.
        const prev = el.previousElementSibling, next = el.nextElementSibling;
        if (prev && prev.classList.contains('day-sep') && (!next || next.classList.contains('day-sep'))) prev.remove();
        el.remove();
    }

    function applyEditedMessage(m) {
        const el = messagesEl.querySelector('.msg[data-id="' + m.id + '"]');
        if (!el) return;
        const bubble = el.querySelector('.bubble');
        if (bubble) bubble.textContent = m.content || '';
        const metaEl = el.querySelector('.msg-meta');
        if (metaEl && !metaEl.querySelector('.edited')) {
            const span = document.createElement('span');
            span.className = 'edited';
            span.textContent = 'düzenlendi';
            metaEl.insertBefore(span, metaEl.firstChild);
        }
    }

    function appendMessage(m) { appendWithDaySep(m); scrollToBottom(); }
    function scrollToBottom() { messagesEl.scrollTop = messagesEl.scrollHeight; }

    function markOutgoingRead() {
        messagesEl.querySelectorAll('.msg.out .ticks').forEach(t => { t.textContent = '✓✓'; t.classList.add('read'); });
    }

    function refreshUnreadDots() {
        document.querySelectorAll('.friend-item').forEach(el => {
            const id = +el.dataset.id;
            const c = unread.get(id) || 0;
            const dot = el.querySelector('.unread-dot');
            if (dot) { if (c > 0) { dot.hidden = false; dot.textContent = c > 9 ? '9+' : c; } else { dot.hidden = true; dot.textContent = ''; } }
            el.classList.toggle('has-unread', c > 0);
        });
        // Sekme başlığında toplam okunmamış rozeti — başka sekmedeyken de fark edilsin.
        let total = 0;
        unread.forEach(c => { total += c; });
        document.title = total > 0 ? '(' + (total > 99 ? '99+' : total) + ') ' + baseTitle : baseTitle;
    }

    // ───────── aktiflik (çevrimiçi / son görülme) ─────────
    function setFriendPresence(id, isOnline, lastSeenAt) {
        const f = friends.get(id);
        if (!f) return;
        f.isOnline = !!isOnline;
        if (lastSeenAt !== undefined) f.lastSeenAt = lastSeenAt;
        const el = document.querySelector('.friend-item[data-id="' + id + '"]');
        if (el) {
            const sdot = el.querySelector('.status-dot');
            if (sdot) sdot.classList.toggle('online', f.isOnline);
            const st = el.querySelector('.friend-status');
            // Önizleme gösteriliyorsa aktiflik metniyle ezme (yeşil nokta zaten günceleniyor).
            if (st && !st.classList.contains('is-preview')) { st.textContent = presenceText(f); st.classList.toggle('online', f.isOnline); }
        }
        if (currentFriend && currentFriend.id === id) updateConvPresence(f);
    }

    function updateConvPresence(f) {
        if (!convPresence) return;
        convPresence.textContent = presenceText(f);
        convPresence.classList.toggle('online', !!(f && f.isOnline));
    }

    function setConvAvatar(f) {
        if (!convAvatar) return;
        const name = f.displayName || f.username;
        if (f.avatarUrl) {
            convAvatar.style.backgroundImage = "url('" + mediaUrl(f.avatarUrl) + "')";
            convAvatar.textContent = '';
        } else {
            convAvatar.style.backgroundImage = '';
            convAvatar.textContent = initial(name);
        }
        convAvatar.hidden = false;
    }

    // ───────── composer (metin) ─────────
    composer.addEventListener('submit', async (e) => {
        e.preventDefault();
        const text = messageInput.value.trim();
        if (!text || !currentFriend) return;
        messageInput.value = '';
        try { await connection.invoke('SendMessage', currentFriend.id, text); }
        catch (err) { toast('Mesaj gönderilemedi.', 'error'); messageInput.value = text; }
    });

    messageInput.addEventListener('input', () => {
        if (!currentFriend || typingThrottle) return;
        connection.invoke('Typing', currentFriend.id).catch(() => { });
        typingThrottle = setTimeout(() => { typingThrottle = null; }, 1500);
    });

    function showTyping() {
        typingIndicator.hidden = false;
        clearTimeout(typingClearTimer);
        typingClearTimer = setTimeout(() => { typingIndicator.hidden = true; }, 2500);
    }

    // ───────── ses kaydı (voice note) ─────────
    let mediaRecorder = null, audioChunks = [], recStartTs = 0, recInterval = null;

    // Birleşik "➕ Ekle" butonu: kayıt sürüyorsa durdur (⏹), değilse Görsel/Ses menüsünü aç-kapat.
    if (attachMenuBtn && attachMenu) {
        attachMenuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (mediaRecorder && mediaRecorder.state === 'recording') { stopRecording(); return; }
            if (!currentFriend) { toast('Önce bir arkadaş seç.', 'error'); return; }
            var cm = $('convMenu'); if (cm) cm.hidden = true;
            var km = $('callMenu'); if (km) km.hidden = true;
            attachMenu.hidden = !attachMenu.hidden;
        });
        document.addEventListener('click', function () { attachMenu.hidden = true; });
        attachMenu.addEventListener('click', function (e) { e.stopPropagation(); });
        attachMenu.querySelectorAll('[data-add]').forEach(function (b) {
            b.addEventListener('click', function () {
                attachMenu.hidden = true;
                if (!currentFriend) { toast('Önce bir arkadaş seç.', 'error'); return; }
                if (b.dataset.add === 'media') { var inp = $('attachInput'); if (inp) inp.click(); }
                else if (b.dataset.add === 'voice') startRecording();
            });
        });
    }

    async function startRecording() {
        if (!navigator.mediaDevices || !window.MediaRecorder) { toast('Tarayıcı ses kaydını desteklemiyor.', 'error'); return; }
        let stream;
        try { stream = await navigator.mediaDevices.getUserMedia({ audio: true }); }
        catch (e) { toast('Mikrofon izni gerekli.', 'error'); return; }

        audioChunks = [];
        mediaRecorder = new MediaRecorder(stream);
        mediaRecorder.ondataavailable = (e) => { if (e.data.size > 0) audioChunks.push(e.data); };
        mediaRecorder.onstop = async () => {
            stream.getTracks().forEach(t => t.stop());
            const blob = new Blob(audioChunks, { type: mediaRecorder.mimeType || 'audio/webm' });
            const duration = Math.max(1, Math.round((Date.now() - recStartTs) / 1000));
            await sendAudio(blob, duration);
        };
        mediaRecorder.start();
        recStartTs = Date.now();
        attachMenuBtn.classList.add('recording');   // ＋ → ■ geçişi CSS'te
        recTimer.hidden = false; recTimer.textContent = '0:00';
        recInterval = setInterval(() => {
            const s = Math.floor((Date.now() - recStartTs) / 1000);
            recTimer.textContent = Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0');
        }, 500);
    }

    function stopRecording() {
        if (mediaRecorder && mediaRecorder.state === 'recording') mediaRecorder.stop();
        attachMenuBtn.classList.remove('recording');   // ■ → ＋ geçişi CSS'te
        recTimer.hidden = true; clearInterval(recInterval);
    }

    async function sendAudio(blob, duration) {
        const friendId = currentFriend && currentFriend.id;
        if (!friendId) return;
        const b64 = await blobToBase64(blob);
        const r = await api('/api/v1/message/audio', {
            method: 'POST',
            body: { receiverId: friendId, audioData: b64, audioName: 'voice.webm', durationSeconds: duration }
        });
        if (r && r.success && r.data) { appendMessage(r.data); noteSummary(friendId, r.data); }
        else toast(errOf(r, 'Ses mesajı gönderilemedi.'), 'error');
    }

    function blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => { const s = String(reader.result); resolve(s.substring(s.indexOf(',') + 1)); };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }

    // ───────── foto/video eki ─────────
    const IMAGE_MAX = 10 * 1024 * 1024;   // 10 MB
    const VIDEO_MAX = 30 * 1024 * 1024;   // 30 MB
    const attachInput = $('attachInput');

    if (attachInput) {
        attachInput.addEventListener('change', async () => {
            const file = attachInput.files && attachInput.files[0];
            attachInput.value = '';
            if (!file || !currentFriend) return;
            const isVideo = /^video\//i.test(file.type);
            const isImage = /^image\//i.test(file.type);
            if (!isVideo && !isImage) { toast('Yalnızca foto veya video gönderebilirsiniz.', 'error'); return; }
            const max = isVideo ? VIDEO_MAX : IMAGE_MAX;
            if (file.size > max) {
                toast((isVideo ? 'Video en fazla 30 MB' : 'Fotoğraf en fazla 10 MB') + ' olabilir.', 'error');
                return;
            }
            await sendMedia(file, isVideo ? 'Video' : 'Image');
        });
    }

    async function sendMedia(file, mediaType) {
        const friendId = currentFriend && currentFriend.id;
        if (!friendId) return;
        let duration = null;
        if (mediaType === 'Video') duration = await readVideoDuration(file).catch(() => null);
        toast(mediaType === 'Video' ? 'Video yükleniyor…' : 'Fotoğraf yükleniyor…');
        const b64 = await blobToBase64(file);
        const r = await api('/api/v1/message/media', {
            method: 'POST',
            body: { receiverId: friendId, data: b64, fileName: file.name, mediaType: mediaType, durationSeconds: duration }
        });
        if (r && r.success && r.data) { appendMessage(r.data); noteSummary(friendId, r.data); }
        else toast(errOf(r, 'Medya gönderilemedi.'), 'error');
    }

    function readVideoDuration(file) {
        return new Promise((resolve, reject) => {
            const v = document.createElement('video');
            v.preload = 'metadata';
            v.onloadedmetadata = () => { try { URL.revokeObjectURL(v.src); } catch (e) {} resolve(isFinite(v.duration) ? Math.round(v.duration) : null); };
            v.onerror = reject;
            v.src = URL.createObjectURL(file);
        });
    }

    // ───────── görsel lightbox (tıkla-büyüt) ─────────
    messagesEl.addEventListener('click', (e) => {
        const img = e.target.closest && e.target.closest('.msg-img');
        if (!img) return;
        openLightbox(img.getAttribute('data-full'));
    });
    function openLightbox(src) {
        if (!src) return;
        let ov = document.getElementById('imgLightbox');
        if (!ov) {
            ov = document.createElement('div');
            ov.id = 'imgLightbox';
            ov.className = 'img-lightbox';
            ov.innerHTML = '<img alt="Görsel">';
            ov.addEventListener('click', () => ov.classList.remove('open'));
            document.body.appendChild(ov);
        }
        ov.querySelector('img').src = src;
        ov.classList.add('open');
    }

    // Sayfa GİZLİYKEN (başka sekme/uygulama ama hâlâ açık) gelen mesaj için bildirim göster.
    // Odaktayken in-app yeterli; sayfa tamamen kapalıyken zaten sunucu Web Push atar (çift bildirim olmaz).
    function maybeNotifyForeground(m) {
        if (!document.hidden) return;
        if (!('Notification' in window) || Notification.permission !== 'granted' || !navigator.serviceWorker) return;
        const f = friends.get(m.senderId);
        const name = f ? (f.displayName || f.username) : 'Biri';
        navigator.serviceWorker.ready.then(function (reg) {
            reg.showNotification('Chatural', {
                body: name + ' sana mesaj gönderdi',
                tag: 'chat-message', renotify: true,
                icon: '/icons/icon-192.png', badge: '/icons/icon-192.png',
                data: { url: '/Chat' }
            });
        }).catch(function () { });
    }

    // ───────── SignalR ─────────
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/bff/hubs/chat')   // BFF proxy; JWT'yi YARP ekler, WS URL'inde token yok
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveMessage', (m) => {
        if (currentFriend && m.senderId === currentFriend.id) {
            appendMessage(m);
            api('/api/v1/message/' + currentFriend.id + '/read', { method: 'POST' });
        } else {
            unread.set(m.senderId, (unread.get(m.senderId) || 0) + 1);
            if (!friends.has(m.senderId)) loadFriends().then(refreshUnreadDots);
            else refreshUnreadDots();
        }
        noteSummary(m.senderId, m);
        maybeNotifyForeground(m);
    });

    connection.on('MessageSent', (m) => {
        if (currentFriend && m.receiverId === currentFriend.id) appendMessage(m);
        noteSummary(m.receiverId, m);
    });

    connection.on('MessageError', (msg) => toast(msg, 'error'));

    connection.on('FriendRequestReceived', (req) => {
        loadRequests();
        flashRequests();
        toast(((req.displayName || req.username) || 'Biri') + ' sana arkadaşlık isteği gönderdi.');
    });

    connection.on('FriendRequestAccepted', (friend) => {
        loadFriends();
        loadRequests();   // giden istek artık kabul edildi → "Bekliyor" listesinden düşsün
        toast(((friend.displayName || friend.username) || 'Biri') + ' arkadaşlık isteğini kabul etti.');
    });

    connection.on('UserTyping', (uid) => {
        if (currentFriend && uid === currentFriend.id) showTyping();
    });

    // Karşı taraf mesajlarımı okuduğunda -> çift mavi tik
    connection.on('MessagesRead', (byId) => {
        if (currentFriend && byId === currentFriend.id) markOutgoingRead();
    });

    // Mesaj silindi/düzenlendi (iki tarafa da gelir; gönderenin diğer sekmeleri de eşitlenir).
    connection.on('MessageDeleted', (m) => {
        const other = m.senderId === me ? m.receiverId : m.senderId;
        if (currentFriend && other === currentFriend.id) removeMessageEl(m.id);
        loadUnread();   // okunmamış sayacı + son mesaj önizlemesi sunucudan tazelenir
    });

    connection.on('MessageEdited', (m) => {
        const other = m.senderId === me ? m.receiverId : m.senderId;
        if (currentFriend && other === currentFriend.id) applyEditedMessage(m);
        loadUnread();   // son mesaj düzenlendiyse kenar çubuğu önizlemesi de değişir
    });

    // ───────── aktiflik bildirimleri ─────────
    // Bağlanınca: o an çevrimiçi olan arkadaşların id listesi.
    connection.on('OnlineFriends', (ids) => {
        const set = new Set(ids || []);
        friends.forEach((f, id) => setFriendPresence(id, set.has(id), undefined));
    });
    connection.on('UserOnline', (uid) => setFriendPresence(uid, true, null));
    connection.on('UserOffline', (uid, lastSeen) => setFriendPresence(uid, false, lastSeen));

    // call.js'in arama sinyalleşmesi için paylaşılan köprü (aynı SignalR bağlantısı).
    window.ChatBridge = {
        connection: connection,
        api: api,
        toast: toast,
        mediaUrl: mediaUrl,
        initial: initial,
        esc: esc,
        relTime: relTime,
        presenceText: presenceText,
        me: me,
        friend: function (id) { return friends.get(id) || null; },
        currentFriendId: function () { return currentFriend ? currentFriend.id : null; }
    };

    function setConn(msg, show) {
        if (show) { connStatus.textContent = msg; connStatus.hidden = false; }
        else { connStatus.hidden = true; }
    }

    connection.onreconnecting(() => setConn('Yeniden bağlanılıyor…', true));
    connection.onreconnected(() => setConn('', false));
    // Otomatik reconnect denemeleri tükenince pes etme; kendimiz baştan bağlanmayı sürdür.
    connection.onclose(() => { setConn('Bağlantı kesildi. Yeniden bağlanılıyor…', true); setTimeout(start, 3000); });

    async function start() {
        try { await connection.start(); setConn('', false); }
        catch (e) { setConn('Bağlantı kurulamadı, tekrar deneniyor…', true); setTimeout(start, 3000); }
    }

    // ───────── profil modalı tetikleyicileri ─────────
    (function () {
        var meAvatar = document.getElementById('meAvatar');
        // Kendi avatarıma tıklayınca profil modalı açılır (fotoğraf değiştirme modal içinde).
        if (meAvatar) meAvatar.addEventListener('click', function () { if (window.Profile) window.Profile.open(me); });
        // Sohbet başlığındaki avatara tıklayınca o arkadaşın profili açılır.
        if (convAvatar) convAvatar.addEventListener('click', function () {
            var id = currentFriend && currentFriend.id;
            if (id && window.Profile) window.Profile.open(id);
        });
        // Mobil: "‹ geri" → arkadaş listesine dön (sohbet durumu korunur).
        var convBack = document.getElementById('convBack');
        if (convBack) convBack.addEventListener('click', function () {
            if (chatApp) chatApp.classList.remove('show-conversation');
        });
    })();

    // ───────── avatar yükleme (profil modalındaki "Fotoğrafı değiştir" tetikler) ─────────
    (function () {
        var meAvatar = document.getElementById('meAvatar');
        var input = document.getElementById('avatarFileInput');
        if (!input) return;
        input.addEventListener('change', async function () {
            var file = input.files && input.files[0];
            input.value = '';
            if (!file) return;
            var b64 = await blobToBase64(file);
            var r = await api('/api/v1/user/me/avatar', { method: 'POST', body: { imageData: b64, imageName: file.name } });
            if (r && r.success && r.data && r.data.avatarUrl) {
                var url = r.data.avatarUrl;
                meAvatar.style.backgroundImage = "url('" + mediaUrl(url) + "')";
                meAvatar.style.backgroundSize = 'cover';
                meAvatar.style.backgroundPosition = 'center';
                meAvatar.textContent = '';
                cfg.avatarUrl = url;
                toast('Avatar güncellendi.');
            } else {
                toast(errOf(r, 'Avatar yüklenemedi.'), 'error');
            }
        });
    })();

    // ───────── çağrı seçim menüsü (mobil: tek "Ara" → sesli/görüntülü) ─────────
    (function () {
        var btn = $('callMenuBtn');
        var menu = $('callMenu');
        if (!btn || !menu) return;

        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            var other = $('convMenu'); if (other) other.hidden = true;   // diğer menüyü kapat
            menu.hidden = !menu.hidden;
        });
        document.addEventListener('click', function () { menu.hidden = true; });
        menu.addEventListener('click', function (e) { e.stopPropagation(); });

        menu.querySelectorAll('[data-act]').forEach(function (b) {
            b.addEventListener('click', function () {
                menu.hidden = true;
                // Mevcut (mobilde görsel gizli) çağrı butonlarını programatik tetikle → call.js startCall.
                var id = b.dataset.act === 'call-video' ? 'callVideoBtn' : 'callAudioBtn';
                var target = document.getElementById(id);
                if (target) target.click();
            });
        });
    })();

    // ───────── konuşma menüsü (engelle / şikayet et) ─────────
    (function () {
        var menuBtn = $('convMenuBtn');
        var menu = $('convMenu');
        if (!menuBtn || !menu) return;

        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            var other = $('callMenu'); if (other) other.hidden = true;   // çağrı menüsünü kapat
            menu.hidden = !menu.hidden;
        });
        document.addEventListener('click', function () { menu.hidden = true; });
        menu.addEventListener('click', function (e) { e.stopPropagation(); });

        menu.querySelectorAll('[data-act]').forEach(function (b) {
            b.addEventListener('click', function () {
                menu.hidden = true;
                if (b.dataset.act === 'block') blockCurrentFriend();
                else if (b.dataset.act === 'report') reportCurrentFriend();
            });
        });
    })();

    async function blockCurrentFriend() {
        if (!currentFriend) return;
        if (!window.confirm(currentFriend.name + ' adlı kişiyi engellemek istediğinize emin misiniz? Arkadaşlığınız kaldırılır.')) return;
        var r = await api('/api/v1/friend/' + currentFriend.id + '/block', { method: 'POST' });
        if (r && r.success) {
            toast(currentFriend.name + ' engellendi.');
            currentFriend = null;
            convTitle.textContent = 'Sohbet etmek için bir arkadaş seç';
            if (convPresence) { convPresence.textContent = ''; convPresence.classList.remove('online'); }
            if (convAvatar) { convAvatar.hidden = true; convAvatar.style.backgroundImage = ''; convAvatar.textContent = ''; }
            if (chatApp) chatApp.classList.remove('show-conversation');   // mobil: listeye dön
            composer.hidden = true;
            messagesEl.innerHTML = '<div class="empty-hint">Henüz bir sohbet seçilmedi.</div>';
            ['callAudioBtn', 'callVideoBtn', 'callMenuBtn', 'convMenuBtn'].forEach(function (cid) { var b = $(cid); if (b) b.hidden = true; });
            loadFriends();
        } else {
            toast(errOf(r, 'Engellenemedi.'), 'error');
        }
    }

    async function reportCurrentFriend() {
        if (!currentFriend) return;
        var reason = window.prompt('Şikayet nedeni (opsiyonel):', '');
        if (reason === null) return; // iptal
        var r = await api('/api/v1/report', {
            method: 'POST',
            body: { targetType: 'User', reportedUserId: currentFriend.id, reason: reason }
        });
        if (r && r.success) toast('Şikayetiniz alındı. İncelenecektir.');
        else toast(errOf(r, 'Şikayet gönderilemedi.'), 'error');
    }

    // ───────── üyelik sözleşmesi (eski üyeler için zorunlu onay) ─────────
    (function () {
        if (cfg.agreementAccepted) return;
        var ov = document.getElementById('agreementOverlay');
        var btn = document.getElementById('agreementAccept');
        if (!ov || !btn) return;
        ov.classList.add('open');
        btn.addEventListener('click', async function () {
            btn.disabled = true;
            var r = await api('/api/v1/user/me/accept-agreement', { method: 'POST' });
            if (r && r.success) {
                cfg.agreementAccepted = true;
                // Sunum oturumunu da işaretle ki sayfa yenilenince modal tekrar gelmesin.
                try {
                    var tok = document.querySelector('input[name="__RequestVerificationToken"]');
                    var body = new URLSearchParams();
                    if (tok) body.set('__RequestVerificationToken', tok.value);
                    await fetch('/Chat/AgreementAccepted', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: body.toString()
                    });
                } catch (e) { /* oturum güncellenemezse en kötü ihtimalle bir sonraki yenilemede tekrar sorar */ }
                ov.classList.remove('open');
                toast('Teşekkürler, sözleşme onaylandı.');
            } else {
                btn.disabled = false;
                toast(errOf(r, 'Onay kaydedilemedi.'), 'error');
            }
        });
    })();

    (async function () {
        await loadFriends();
        await loadUnread();
        loadRequests();
        start();
    })();
})();
