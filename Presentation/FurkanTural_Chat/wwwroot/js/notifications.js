/* Chatural — Web Push aboneliği: izin al, VAPID ile abone ol, /bff/push'a kaydet. */
(function () {
    'use strict';

    var btn = document.getElementById('notifyToggleBtn');
    if (!btn) return;

    var supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    if (!supported) { btn.hidden = true; return; }

    btn.hidden = false;

    var BEAT_KEY = 'ft.push.beat';
    var BEAT_INTERVAL_MS = 24 * 60 * 60 * 1000;
    var LOGOUT_WAIT_MS = 2000;

    function toast(msg, type) {
        if (window.showToast) window.showToast(type === 'error' ? 'error' : 'info', type === 'error' ? 'Hata' : 'Bildirim', msg);
    }

    // base64url (VAPID public key) → Uint8Array (applicationServerKey için)
    function urlB64ToUint8(base64) {
        var pad = '='.repeat((4 - base64.length % 4) % 4);
        var b64 = (base64 + pad).replace(/-/g, '+').replace(/_/g, '/');
        var raw = atob(b64);
        var arr = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) arr[i] = raw.charCodeAt(i);
        return arr;
    }

    function setState(state) {
        // 'on' | 'off' | 'blocked'
        btn.dataset.state = state;
        btn.classList.toggle('active', state === 'on');
        btn.title = state === 'on' ? 'Bildirimler açık (kapatmak için tıkla)'
            : state === 'blocked' ? 'Bildirimler tarayıcı ayarlarında engelli'
                : 'Bildirimleri aç';
    }

    function csrfToken() {
        var meta = document.querySelector('meta[name="ft-antiforgery"]');
        return meta ? meta.content : '';
    }

    function getReg() { return navigator.serviceWorker.ready; }
    function currentSub() { return getReg().then(function (reg) { return reg.pushManager.getSubscription(); }); }

    function markBeat(at) {
        try {
            if (at) localStorage.setItem(BEAT_KEY, String(at));
            else localStorage.removeItem(BEAT_KEY);
        } catch (e) { }
    }

    function beatDue() {
        var last = 0;
        try { last = parseInt(localStorage.getItem(BEAT_KEY) || '0', 10) || 0; } catch (e) { }
        return Date.now() - last >= BEAT_INTERVAL_MS;
    }

    async function register(sub) {
        var json = sub.toJSON();
        try {
            var r = await fetch('/bff/api/v1/push/subscribe', {
                method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken() },
                body: JSON.stringify({ endpoint: sub.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth, userAgent: navigator.userAgent })
            });
            if (r.ok) markBeat(Date.now());
            return r.ok;
        } catch (e) { return false; }
    }

    async function forget(sub) {
        try {
            await fetch('/bff/api/v1/push/unsubscribe', {
                method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken() }, keepalive: true,
                body: JSON.stringify({ endpoint: sub.endpoint })
            });
        } catch (e) { }
        try { await sub.unsubscribe(); } catch (e) { }
        markBeat(0);
    }

    async function enable() {
        var perm = await Notification.requestPermission();
        if (perm !== 'granted') { setState(perm === 'denied' ? 'blocked' : 'off'); if (perm === 'denied') toast('Bildirim izni reddedildi.', 'error'); return; }

        // VAPID açık anahtarını sunucudan al (ortama göre değişir → sabitlemiyoruz)
        var keyRes;
        try { keyRes = await fetch('/bff/api/v1/push/vapid-public-key'); } catch (e) { keyRes = null; }
        if (!keyRes || !keyRes.ok) { toast('Bildirim altyapısı şu an kullanılamıyor.', 'error'); setState('off'); return; }
        var keyJson = await keyRes.json();
        var pub = keyJson && keyJson.data;
        if (!pub) { toast('Bildirimler şu an yapılandırılmamış.', 'error'); setState('off'); return; }

        var reg = await getReg();
        var sub;
        try {
            sub = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: urlB64ToUint8(pub) });
        } catch (e) { toast('Bildirime abone olunamadı.', 'error'); setState('off'); return; }

        var ok = await register(sub);

        if (ok) { setState('on'); toast('Bildirimler açıldı. Çevrimdışıyken de mesajların ulaşacak.'); }
        else { try { await sub.unsubscribe(); } catch (e) { } setState('off'); toast('Abonelik kaydedilemedi.', 'error'); }
    }

    async function disable() {
        var sub = await currentSub();
        if (sub) await forget(sub);
        setState('off');
        toast('Bildirimler kapatıldı.');
    }

    btn.addEventListener('click', async function () {
        if (Notification.permission === 'denied') { setState('blocked'); toast('Bildirimler tarayıcı ayarlarından engellenmiş. Oradan izin vermelisin.', 'error'); return; }
        var sub = await currentSub();
        if (sub) await disable(); else await enable();
    });

    var logoutForm = document.querySelector('.logout-form');
    if (logoutForm) {
        logoutForm.addEventListener('submit', function (e) {
            if (logoutForm.dataset.pushCleared === '1') return;
            e.preventDefault();

            var finished = false;
            function proceed() {
                if (finished) return;
                finished = true;
                logoutForm.dataset.pushCleared = '1';
                logoutForm.submit();
            }

            setTimeout(proceed, LOGOUT_WAIT_MS);
            currentSub()
                .then(function (sub) { return sub ? forget(sub) : null; })
                .catch(function () { })
                .then(proceed);
        });
    }

    // Açılışta mevcut durumu yansıt.
    (async function () {
        if (Notification.permission === 'denied') { setState('blocked'); return; }
        var sub = await currentSub();
        setState(sub ? 'on' : 'off');
        if (sub && Notification.permission === 'granted' && beatDue()) await register(sub);
    })();
})();
