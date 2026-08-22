// İki aşamalı profil modalı:
//  Aşama 1 — kişi bilgileri + profil fotoğrafı.
//  Aşama 2 — fotoğrafa tıklayınca büyütme; görsel dışına tıklayınca aşama 1'e dönüş.
// Veri: kendi profili window.CHAT'ten, arkadaşlar window.ChatBridge.friend(id)'den gelir.
(function () {
    'use strict';
    var cfg = window.CHAT || {};

    function B() { return window.ChatBridge || {}; }
    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }
    function initial(name) { return ((name || '?').trim().charAt(0) || '?').toUpperCase(); }
    function mediaUrl(v) { var b = B(); return b.mediaUrl ? b.mediaUrl(v) : (v || ''); }
    function fmtDate(iso) { return window.FtTime ? FtTime.date(iso) : (function () { try { return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric', timeZone: 'Europe/Istanbul' }); } catch (e) { return ''; } })(); }

    var overlay = document.createElement('div');
    overlay.className = 'profile-overlay';
    overlay.innerHTML =
        '<div class="profile-card" role="dialog" aria-modal="true">' +
            '<div class="profile-photo" data-photo title="Büyüt"></div>' +
            '<div class="profile-name" data-name></div>' +
            '<div class="profile-username" data-username></div>' +
            '<div class="profile-rows" data-rows></div>' +
            '<button type="button" class="btn-outline profile-change" data-change hidden>Fotoğrafı değiştir</button>' +
        '</div>';
    document.body.appendChild(overlay);

    var zoom = document.createElement('div');
    zoom.className = 'profile-zoom';
    document.body.appendChild(zoom);

    var photoEl = overlay.querySelector('[data-photo]');
    var nameEl = overlay.querySelector('[data-name]');
    var userEl = overlay.querySelector('[data-username]');
    var rowsEl = overlay.querySelector('[data-rows]');
    var changeBtn = overlay.querySelector('[data-change]');

    var currentAvatarUrl = null; // tam URL veya null (fallback baş harf)

    function close() { overlay.classList.remove('open'); zoom.classList.remove('open'); }
    function backToCard() { zoom.classList.remove('open'); }

    function openZoom() {
        zoom.innerHTML = '';
        if (currentAvatarUrl) {
            var img = document.createElement('img');
            img.src = currentAvatarUrl;
            img.alt = 'Profil fotoğrafı';
            img.addEventListener('click', function (e) { e.stopPropagation(); }); // görsele tıklamak kapatmaz
            zoom.appendChild(img);
        } else {
            var fb = document.createElement('div');
            fb.className = 'profile-zoom-fallback';
            fb.textContent = photoEl.textContent || '?';
            fb.addEventListener('click', function (e) { e.stopPropagation(); });
            zoom.appendChild(fb);
        }
        zoom.classList.add('open');
    }

    // Kart dışına tıkla → kapat (aşama 1).
    overlay.addEventListener('click', function (e) { if (e.target === overlay) close(); });
    // Fotoğrafa tıkla → büyüt (aşama 2).
    photoEl.addEventListener('click', openZoom);
    // Görsel dışına tıkla → aşama 1'e dön.
    zoom.addEventListener('click', backToCard);
    // "Fotoğrafı değiştir" → gizli dosya girişini tetikle (yükleme chat.js'te).
    changeBtn.addEventListener('click', function () {
        var inp = document.getElementById('avatarFileInput');
        if (inp) { close(); inp.click(); }
    });
    // Esc: önce büyütmeyi, sonra modalı kapatır.
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if (zoom.classList.contains('open')) backToCard();
        else if (overlay.classList.contains('open')) close();
    });

    function row(lbl, val, online) {
        return '<div class="profile-row"><span class="lbl">' + esc(lbl) + '</span>' +
               '<span class="val' + (online ? ' online' : '') + '">' + esc(val) + '</span></div>';
    }

    function open(userId) {
        var b = B();
        var isSelf = (userId === cfg.userId) || (userId === b.me);
        var name, username, avatarUrl, rowsHtml = '';

        if (isSelf) {
            username = cfg.username || '';
            name = cfg.displayName || cfg.username || '';
            avatarUrl = cfg.avatarUrl || '';
        } else {
            var f = b.friend ? b.friend(userId) : null;
            if (!f) return;
            username = f.username || '';
            name = f.displayName || f.username || '';
            avatarUrl = f.avatarUrl || '';
            var pres = b.presenceText ? b.presenceText(f) : (f.isOnline ? 'çevrimiçi' : 'çevrimdışı');
            rowsHtml += row('Durum', pres, !!f.isOnline);
            if (f.since) rowsHtml += row('Arkadaşlık', fmtDate(f.since), false);
        }

        currentAvatarUrl = avatarUrl ? mediaUrl(avatarUrl) : null;
        if (currentAvatarUrl) { photoEl.style.backgroundImage = "url('" + currentAvatarUrl + "')"; photoEl.textContent = ''; }
        else { photoEl.style.backgroundImage = ''; photoEl.textContent = initial(name); }

        nameEl.textContent = name;
        userEl.textContent = '@' + username;
        rowsEl.innerHTML = rowsHtml;
        changeBtn.hidden = !isSelf;

        zoom.classList.remove('open');
        overlay.classList.add('open');
    }

    window.Profile = { open: open };
})();
