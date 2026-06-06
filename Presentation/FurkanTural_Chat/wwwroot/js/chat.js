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
    const typingIndicator = $('typingIndicator');
    const connStatus = $('connStatus');
    const recBtn = $('recBtn');
    const recTimer = $('recTimer');

    let currentFriend = null;          // { id, name }
    const friends = new Map();         // friendUserId -> friend dto
    const unread = new Map();          // friendUserId -> count
    let searchTimer = null;
    let typingThrottle = null;
    let typingClearTimer = null;

    // ───────── helpers ─────────
    function esc(s) { const d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }
    function initial(name) { return ((name || '?').trim().charAt(0) || '?').toUpperCase(); }
    function fmtTime(iso) { try { return new Date(iso).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }); } catch (e) { return ''; } }
    function toast(msg, type) { if (window.showToast) window.showToast(type === 'error' ? 'error' : 'info', type === 'error' ? 'Hata' : 'Bilgi', msg); }
    // Yeni kayıtlar göreli yol ('chats/images/..') taşır → apiBase + '/' + value; eski kayıtlar düz dosya adı → images/uploads.
    function mediaUrl(value) {
        if (!value) return '';
        return value.indexOf('/') >= 0 ? API + '/' + value : API + '/images/uploads/' + value;
    }
    function avatarMarkup(name, avatarUrl, cls) {
        cls = cls || 'avatar sm';
        if (avatarUrl) {
            var url = mediaUrl(avatarUrl);
            return '<div class="' + cls + '" style="background-image:url(\'' + url + '\');background-size:cover;background-position:center;"></div>';
        }
        return '<div class="' + cls + '">' + esc(initial(name)) + '</div>';
    }

    async function api(path, opts) {
        opts = opts || {};
        opts.headers = Object.assign({ 'Authorization': 'Bearer ' + cfg.token }, opts.headers || {});
        if (opts.body && typeof opts.body !== 'string') {
            opts.headers['Content-Type'] = 'application/json';
            opts.body = JSON.stringify(opts.body);
        }
        let res;
        try { res = await fetch(API + path, opts); }
        catch (e) { return null; }
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        try { return await res.json(); } catch (e) { return null; }
    }

    function errOf(r, fallback) { return (r && r.errors && r.errors[0]) || (r && r.message) || fallback; }

    // ───────── friends ─────────
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
                avatarMarkup(name, f.avatarUrl) +
                '<div class="friend-meta"><div class="friend-name">' + esc(name) + '</div>' +
                '<div class="friend-sub">@' + esc(f.username) + '</div></div>' +
                '<span class="unread-dot" hidden></span>';
            div.addEventListener('click', () => openConversation(f.friendUserId));
            friendsList.appendChild(div);
        });
        refreshUnreadDots();
    }

    // Okunmamış sayılarını sunucudan tohumla (sayfa yüklendiğinde / yenilendiğinde)
    async function loadUnread() {
        const r = await api('/api/v1/message/conversations');
        if (!(r && r.success)) return;
        (r.data || []).forEach(c => { if (c.unreadCount > 0) unread.set(c.friendUserId, c.unreadCount); });
        refreshUnreadDots();
    }

    // ───────── search ─────────
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

    // ───────── requests ─────────
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

    // ───────── conversation ─────────
    async function openConversation(friendId) {
        const f = friends.get(friendId);
        if (!f) return;
        currentFriend = { id: friendId, name: f.displayName || f.username };
        document.querySelectorAll('.friend-item').forEach(el => el.classList.toggle('active', +el.dataset.id === friendId));
        convTitle.textContent = currentFriend.name;
        typingIndicator.hidden = true;
        composer.hidden = false;
        messagesEl.innerHTML = '<div class="empty-hint">Yükleniyor…</div>';

        const r = await api('/api/v1/message/conversation/' + friendId);
        if (r && r.success) renderMessages(r.data || []);
        else messagesEl.innerHTML = '<div class="empty-hint">Mesajlar yüklenemedi.</div>';

        unread.set(friendId, 0);
        refreshUnreadDots();
        api('/api/v1/message/' + friendId + '/read', { method: 'POST' }); // okundu -> gönderene bildirilir
        messageInput.focus();
        ['callAudioBtn', 'callVideoBtn', 'convMenuBtn'].forEach(function (cid) { var b = $(cid); if (b) b.hidden = false; });
        document.dispatchEvent(new CustomEvent('chat:conversationchanged', { detail: { friendId: friendId } }));
    }

    function renderMessages(list) {
        messagesEl.innerHTML = '';
        if (!list.length) { messagesEl.innerHTML = '<div class="empty-hint">Henüz mesaj yok. İlk mesajı sen gönder!</div>'; return; }
        list.forEach(m => messagesEl.appendChild(messageEl(m)));
        scrollToBottom();
    }

    function messageEl(m) {
        const out = m.senderId === me;
        const div = document.createElement('div');
        div.className = 'msg ' + (out ? 'out' : 'in');

        const type = (m.messageType || 'text').toLowerCase();
        let bubble;
        if (type === 'audio' && m.attachmentUrl) {
            bubble = '<div class="bubble audio"><audio controls preload="metadata" src="' + mediaUrl(m.attachmentUrl) + '"></audio></div>';
        } else if (type === 'image' && m.attachmentUrl) {
            const iurl = mediaUrl(m.attachmentUrl);
            bubble = '<div class="bubble media"><img class="msg-img" src="' + iurl + '" alt="Görsel" loading="lazy" data-full="' + iurl + '"></div>';
        } else if (type === 'video' && m.attachmentUrl) {
            bubble = '<div class="bubble media"><video class="msg-video" controls preload="metadata" src="' + mediaUrl(m.attachmentUrl) + '"></video></div>';
        } else {
            bubble = '<div class="bubble">' + esc(m.content) + '</div>';
        }

        let meta = '<span>' + esc(fmtTime(m.createdAt)) + '</span>';
        if (out) meta += ' <span class="ticks' + (m.isRead ? ' read' : '') + '">' + (m.isRead ? '✓✓' : '✓') + '</span>';

        div.innerHTML = bubble + '<div class="msg-meta">' + meta + '</div>';
        return div;
    }

    function appendMessage(m) { messagesEl.appendChild(messageEl(m)); scrollToBottom(); }
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

    if (recBtn) {
        recBtn.addEventListener('click', async () => {
            if (!currentFriend) { toast('Önce bir arkadaş seç.', 'error'); return; }
            if (mediaRecorder && mediaRecorder.state === 'recording') { stopRecording(); return; }
            await startRecording();
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
        recBtn.classList.add('recording'); recBtn.textContent = '⏹';
        recTimer.hidden = false; recTimer.textContent = '0:00';
        recInterval = setInterval(() => {
            const s = Math.floor((Date.now() - recStartTs) / 1000);
            recTimer.textContent = Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0');
        }, 500);
    }

    function stopRecording() {
        if (mediaRecorder && mediaRecorder.state === 'recording') mediaRecorder.stop();
        recBtn.classList.remove('recording'); recBtn.textContent = '🎤';
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
        if (r && r.success && r.data) appendMessage(r.data);
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
    const attachBtn = $('attachBtn');
    const attachInput = $('attachInput');

    if (attachBtn && attachInput) {
        attachBtn.addEventListener('click', () => {
            if (!currentFriend) { toast('Önce bir arkadaş seç.', 'error'); return; }
            attachInput.click();
        });
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
        if (r && r.success && r.data) appendMessage(r.data);
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

    // ───────── SignalR ─────────
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(API + '/hubs/chat', { accessTokenFactory: () => cfg.token })
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
    });

    connection.on('MessageSent', (m) => {
        if (currentFriend && m.receiverId === currentFriend.id) appendMessage(m);
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

    // call.js'in arama sinyalleşmesi için paylaşılan köprü (aynı SignalR bağlantısı).
    window.ChatBridge = {
        connection: connection,
        api: api,
        toast: toast,
        mediaUrl: mediaUrl,
        initial: initial,
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
    connection.onclose(() => setConn('Bağlantı kesildi. Sayfayı yenileyin.', true));

    async function start() {
        try { await connection.start(); setConn('', false); }
        catch (e) { setConn('Bağlantı kurulamadı, tekrar deneniyor…', true); setTimeout(start, 3000); }
    }

    // ───────── avatar (kendi profilim) ─────────
    (function () {
        var meAvatar = document.getElementById('meAvatar');
        var input = document.getElementById('avatarFileInput');
        if (!meAvatar || !input) return;
        meAvatar.addEventListener('click', function () { input.click(); });
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

    // ───────── konuşma menüsü (engelle / şikayet et) ─────────
    (function () {
        var menuBtn = $('convMenuBtn');
        var menu = $('convMenu');
        if (!menuBtn || !menu) return;

        menuBtn.addEventListener('click', function (e) { e.stopPropagation(); menu.hidden = !menu.hidden; });
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
            composer.hidden = true;
            messagesEl.innerHTML = '<div class="empty-hint">Henüz bir sohbet seçilmedi.</div>';
            ['callAudioBtn', 'callVideoBtn', 'convMenuBtn'].forEach(function (cid) { var b = $(cid); if (b) b.hidden = true; });
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

    // ───────── init ─────────
    (async function () {
        await loadFriends();
        await loadUnread();
        loadRequests();
        start();
    })();
})();
