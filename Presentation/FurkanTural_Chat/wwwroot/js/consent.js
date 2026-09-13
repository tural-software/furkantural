// Çerez onay katmanını sunucu açık basar (bkz. _Layout); burası yalnızca onayı alır ve saklar.
(function () {
    'use strict';
    var notice = document.getElementById('consentNotice');
    if (!notice) return;

    function dismiss() {
        try { localStorage.setItem('ft.consent', '1'); } catch (e) { }
        document.cookie = 'ft.consent=1; Max-Age=31536000; Path=/; SameSite=Lax' +
            (location.protocol === 'https:' ? '; Secure' : '');
        notice.hidden = true;
    }

    var okBtn = document.getElementById('consentOk');
    if (okBtn) okBtn.addEventListener('click', dismiss);
})();

// "Geri dön" düğmeleri: CSP (nonce) altında inline onclick yasak olduğundan delegasyonla bağlanır.
(function () {
    'use strict';
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-history-back]');
        if (btn) { e.preventDefault(); history.back(); }
    });
})();
