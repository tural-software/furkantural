/**
 * Turnstile kapısı — doğrulama tamamlanmadan form gönderilemez.
 *
 * Cloudflare betiği async yüklendiği için gizli alan sayfa açılışında boştur.
 * Gönder düğmesi bu yüzden kilitli başlar ve yalnızca token geldiğinde açılır;
 * aksi hâlde kullanıcı doğrulama bitmeden gönderir, sunucu haklı olarak reddeder
 * ve suçu kullanıcıya atan bir hata görünür.
 *
 * Token tek kullanımlıktır ve yaklaşık beş dakikada eskir. Süre dolduğunda ya da
 * doğrulama hata verdiğinde eldeki değer artık geçersizdir: alan temizlenir ve
 * düğme yeni token gelene kadar yeniden kilitlenir.
 */
(function () {
    'use strict';

    var BEKLEME_MS = 15000;
    var zamanlayici = null;

    function alan()   { return document.getElementById('turnstileToken'); }
    function dugme()  { return document.querySelector('form[data-auth-form] button[type="submit"]'); }
    function ipucu()  { return document.getElementById('turnstileHint'); }

    function ipucuYaz(metin) {
        var el = ipucu();
        if (el) el.textContent = metin || '';
    }

    function kilitle() {
        var b = dugme();
        if (b) b.disabled = true;
    }

    function ac() {
        var b = dugme();
        if (b) b.disabled = false;
    }

    function temizle() {
        var el = alan();
        if (el) el.value = '';
    }

    window.onTurnstileSuccess = function (token) {
        if (zamanlayici) { clearTimeout(zamanlayici); zamanlayici = null; }
        var el = alan();
        if (el) el.value = token || '';
        ipucuYaz('');
        ac();
    };

    window.onTurnstileExpired = function () {
        temizle();
        kilitle();
        ipucuYaz('Doğrulamanın süresi doldu, yenileniyor…');
        if (window.turnstile) { try { window.turnstile.reset(); } catch (e) { /* yoksay */ } }
    };

    window.onTurnstileError = function () {
        temizle();
        kilitle();
        ipucuYaz('Robot doğrulaması yüklenemedi. Sayfayı yenileyin; sorun sürerse reklam engelleyicinizi kontrol edin.');
    };

    window.ftTurnstileReset = function () {
        temizle();
        kilitle();
        if (window.turnstile) { try { window.turnstile.reset(); } catch (e) { /* yoksay */ } }
    };

    if (alan() && dugme()) {
        zamanlayici = setTimeout(function () {
            if (!alan().value) window.onTurnstileError();
        }, BEKLEME_MS);
    }
})();
