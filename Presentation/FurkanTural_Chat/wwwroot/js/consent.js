// Çerez onay modalı: onay çereze ve localStorage'a yazılır; katmanı sunucu basmadığı sürece hiç görünmez.
(function () {
    'use strict';
    var KEY = 'ft.consent';
    var COOKIE = /(?:^|;\s*)ft\.consent=1(?:;|$)/;
    var overlay = document.getElementById('consentOverlay');
    if (!overlay) return;

    function accepted() {
        if (COOKIE.test(document.cookie)) return true;
        try { return localStorage.getItem(KEY) === '1'; } catch (e) { return false; }
    }
    function remember() {
        try { localStorage.setItem(KEY, '1'); } catch (e) { }
        document.cookie = 'ft.consent=1; Max-Age=31536000; Path=/; SameSite=Lax' +
            (location.protocol === 'https:' ? '; Secure' : '');
    }
    function accept() {
        remember();
        overlay.classList.remove('open');
    }

    // Açık onay zorunlu: modal dışına tıklamak kapatmaz.
    if (accepted()) remember();
    else overlay.classList.add('open');

    var okBtn = document.getElementById('consentOk');
    if (okBtn) okBtn.addEventListener('click', accept);
})();

// "Geri dön" düğmeleri: CSP (nonce) altında inline onclick yasak olduğundan delegasyonla bağlanır.
(function () {
    'use strict';
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-history-back]');
        if (btn) { e.preventDefault(); history.back(); }
    });
})();
