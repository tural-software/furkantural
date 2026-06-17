/*
 * İstemci-tarafı loglama: yakalanmayan hatalar + açıkça gönderilen hata/uyarı/bilgi
 * logları same-origin '/client-log' ucuna POST edilir; sunucu app-token ile API'ye iletir.
 * En erken yüklenir (head) ki sayfa başındaki hatalar da yakalansın.
 */
(function () {
    'use strict';

    var ENDPOINT = '/client-log';
    var MAX_PER_MIN = 20;          // dakika başına üst sınır (DB'yi sel basmasın)
    var DEDUP_MS = 10000;          // aynı imza bu süre içinde tekrar gönderilmez
    var MAX_MSG = 2000, MAX_DETAIL = 8000;

    var recent = {};               // imza -> son gönderim zamanı
    var windowStart = Date.now();
    var windowCount = 0;
    var sending = false;           // loglama sırasında oluşan hatayı tekrar loglamayı önle

    function clip(s, n) { s = (s == null ? '' : String(s)); return s.length > n ? s.slice(0, n) : s; }

    function allow(signature) {
        var now = Date.now();
        if (now - windowStart > 60000) { windowStart = now; windowCount = 0; }
        if (windowCount >= MAX_PER_MIN) return false;
        var last = recent[signature];
        if (last && (now - last) < DEDUP_MS) return false;
        recent[signature] = now;
        windowCount++;
        return true;
    }

    function send(level, message, detail) {
        if (sending) return;
        message = clip(message, MAX_MSG);
        if (!message) return;
        detail = clip(detail, MAX_DETAIL);
        var signature = level + '|' + message;
        if (!allow(signature)) return;

        var payload = JSON.stringify({
            level: level,
            message: message,
            detail: detail,
            path: location.pathname + location.search
        });

        sending = true;
        try {
            // keepalive: sayfa kapanırken bile gönderim tamamlanır.
            fetch(ENDPOINT, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: payload,
                keepalive: true,
                credentials: 'same-origin'
            }).catch(function () {});
        } catch (e) {
            try { if (navigator.sendBeacon) navigator.sendBeacon(ENDPOINT, new Blob([payload], { type: 'application/json' })); } catch (e2) {}
        } finally {
            sending = false;
        }
    }

    window.ClientLog = {
        error: function (message, detail) { send('Error', message, detail); },
        warn:  function (message, detail) { send('Warning', message, detail); },
        info:  function (message, detail) { send('Information', message, detail); }
    };

    function isSameOrigin(url) {
        try { return new URL(url, location.href).origin === location.origin; } catch (e) { return false; }
    }

    // ── Yakalanmayan JS hataları ──
    window.addEventListener('error', function (e) {
        if (!e) return;
        // Kaynak (img/script) yükleme hataları.
        if (e.target && e.target !== window && (e.target.src || e.target.href)) {
            var resUrl = e.target.src || e.target.href;
            // 3. taraf gürültüsünü (Cloudflare beacon, ad-blocker'lı scriptler) loglama — bizim hatamız değil.
            if (isSameOrigin(resUrl))
                window.ClientLog.error('Kaynak yüklenemedi: ' + resUrl, e.target.tagName);
            return;
        }
        try { e.preventDefault(); } catch (_) {} // konsola yansımasın
        var msg = e.message || 'Bilinmeyen hata';
        var detail = (e.filename ? e.filename + ':' + e.lineno + ':' + e.colno + '\n' : '') +
                     (e.error && e.error.stack ? e.error.stack : '');
        window.ClientLog.error(msg, detail);
    }, true); // capture: kaynak hatalarını da yakalamak için

    // ── Yakalanmayan promise reddi ──
    window.addEventListener('unhandledrejection', function (e) {
        try { e.preventDefault(); } catch (_) {} // konsola yansımasın
        var r = e ? e.reason : null;
        var msg = (r && (r.message || r.toString())) || 'İşlenmemiş promise reddi';
        var detail = (r && r.stack) ? r.stack : '';
        window.ClientLog.error('UnhandledRejection: ' + msg, detail);
    });

    // ── Kullanıcıya gösterilen hata toast'larını da logla (sessizce; toast yine gösterilir) ──
    // toast.js bu scriptten sonra yüklenebilir → DOMContentLoaded'da sarmalama yap.
    function wrapToast() {
        if (typeof window.showToast !== 'function' || window.showToast.__logged) return;
        var orig = window.showToast;
        window.showToast = function (type, title, msg) {
            if (type === 'error') {
                try { window.ClientLog.error((title ? title + ': ' : '') + (msg || '')); } catch (e) {}
            }
            return orig.apply(this, arguments);
        };
        window.showToast.__logged = true;
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', wrapToast);
    } else {
        wrapToast();
    }
})();
