// Çerez onay modalı: kullanıcı bir kez "Kabul et" deyince localStorage'a yazılır ve bir daha gösterilmez.
(function () {
    'use strict';
    var KEY = 'ft.consent';
    var overlay = document.getElementById('consentOverlay');
    if (!overlay) return;

    function accepted() {
        try { return localStorage.getItem(KEY) === '1'; } catch (e) { return false; }
    }
    function accept() {
        try { localStorage.setItem(KEY, '1'); } catch (e) { }
        overlay.classList.remove('open');
    }

    // Açık onay zorunlu: modal dışına tıklamak kapatmaz.
    if (!accepted()) overlay.classList.add('open');

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
