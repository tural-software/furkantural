/* Chatural — Web Push aboneliği: izin al, VAPID ile abone ol, /bff/push'a kaydet. */
(function () {
    'use strict';

    var btn = document.getElementById('notifyToggleBtn');
    if (!btn) return;

    var supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    if (!supported) { btn.hidden = true; return; }

    btn.hidden = false;

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

    function getReg() { return navigator.serviceWorker.ready; }
    function currentSub() { return getReg().then(function (reg) { return reg.pushManager.getSubscription(); }); }

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

        var json = sub.toJSON();
        var ok = false;
        try {
            var r = await fetch('/bff/api/v1/push/subscribe', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ endpoint: sub.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth, userAgent: navigator.userAgent })
            });
            ok = r.ok;
        } catch (e) { ok = false; }

        if (ok) { setState('on'); toast('Bildirimler açıldı. Çevrimdışıyken de mesajların ulaşacak.'); }
        else { try { await sub.unsubscribe(); } catch (e) {} setState('off'); toast('Abonelik kaydedilemedi.', 'error'); }
    }

    async function disable() {
        var sub = await currentSub();
        if (sub) {
            try {
                await fetch('/bff/api/v1/push/unsubscribe', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ endpoint: sub.endpoint })
                });
            } catch (e) {}
            try { await sub.unsubscribe(); } catch (e) {}
        }
        setState('off');
        toast('Bildirimler kapatıldı.');
    }

    btn.addEventListener('click', async function () {
        if (Notification.permission === 'denied') { setState('blocked'); toast('Bildirimler tarayıcı ayarlarından engellenmiş. Oradan izin vermelisin.', 'error'); return; }
        var sub = await currentSub();
        if (sub) await disable(); else await enable();
    });

    // Açılışta mevcut durumu yansıt.
    (async function () {
        if (Notification.permission === 'denied') { setState('blocked'); return; }
        var sub = await currentSub();
        setState(sub ? 'on' : 'off');
    })();
})();
