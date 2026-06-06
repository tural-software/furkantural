/* Tural Chat — PWA: service worker kaydı + "Uygulamayı yükle" akışı */
(function () {
    'use strict';

    // 1) Service worker kaydı (kök scope, SW script her zaman revalide edilsin)
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' })
                .catch(function (err) { console.warn('SW kaydı başarısız:', err); });
        });
    }

    // 2) Yükle çubuğu (beforeinstallprompt) — kapatılabilir, kalıcı reddetme
    var DISMISS_KEY = 'pwa-install-dismissed';
    var deferredPrompt = null;
    var bar = document.getElementById('pwaInstallBar');
    var btn = document.getElementById('pwaInstallBtn');
    var closeBtn = document.getElementById('pwaInstallClose');

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    }
    function isDismissed() {
        try { return localStorage.getItem(DISMISS_KEY) === '1'; } catch (_) { return false; }
    }

    if (bar && btn && !isStandalone() && !isDismissed()) {
        window.addEventListener('beforeinstallprompt', function (e) {
            e.preventDefault();
            deferredPrompt = e;
            if (!isDismissed()) bar.hidden = false;
        });

        btn.addEventListener('click', function () {
            if (!deferredPrompt) return;
            bar.hidden = true;
            deferredPrompt.prompt();
            deferredPrompt.userChoice.finally(function () {
                deferredPrompt = null;
            });
        });

        // "X" → çubuğu gizle ve bir daha gösterme
        if (closeBtn) {
            closeBtn.addEventListener('click', function () {
                bar.hidden = true;
                deferredPrompt = null;
                try { localStorage.setItem(DISMISS_KEY, '1'); } catch (_) {}
            });
        }

        window.addEventListener('appinstalled', function () {
            deferredPrompt = null;
            bar.hidden = true;
            if (window.showToast) window.showToast('success', 'Yüklendi', 'Tural Chat uygulaması kuruldu.');
        });
    }
})();
