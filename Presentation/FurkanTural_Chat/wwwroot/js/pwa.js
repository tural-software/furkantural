/* Chatural — PWA: service worker kaydı + prompt'lu güncelleme + "Uygulamayı yükle" akışı */
(function () {
    'use strict';

    var swReg = null;
    var updateAccepted = false;   // yalnız kullanıcı onayından sonra sayfayı yenile

    // ───────── Güncelleme çubuğu öğeleri ─────────
    var updateBar = document.getElementById('pwaUpdateBar');
    var updateBtn = document.getElementById('pwaUpdateBtn');
    var updateClose = document.getElementById('pwaUpdateClose');

    function showUpdateBar() { if (updateBar) updateBar.hidden = false; }

    if (updateBtn) {
        updateBtn.addEventListener('click', function () {
            if (updateBar) updateBar.hidden = true;
            if (swReg && swReg.waiting) {
                updateAccepted = true;
                // Bekleyen SW'ye onay gönder → o skipWaiting yapar → controllerchange → reload.
                swReg.waiting.postMessage({ type: 'SKIP_WAITING' });
            }
        });
    }
    if (updateClose) {
        // "Şimdi değil" → yalnız bu oturum için gizle; sürüm hâlâ beklediğinden sonraki açılışta tekrar sorar.
        updateClose.addEventListener('click', function () { if (updateBar) updateBar.hidden = true; });
    }

    // ───────── Service worker kaydı + güncelleme tespiti ─────────
    if ('serviceWorker' in navigator) {
        // Yeni SW kontrolü aldığında YALNIZCA kullanıcı güncellemeyi onayladıysa yenile.
        // (İlk kurulumdaki clients.claim() veya arka plan etkinleşmesi sayfayı sıçratmasın.)
        navigator.serviceWorker.addEventListener('controllerchange', function () {
            if (updateAccepted) window.location.reload();
        });

        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { updateViaCache: 'none' })
                .then(function (reg) {
                    swReg = reg;

                    // Her açılışta sunucuda yeni sürüm var mı diye zorla kontrol et (yüklü PWA güncel kalsın).
                    reg.update().catch(function () {});

                    // Önceki ziyaretten beri hazır bekleyen bir güncelleme varsa hemen sor.
                    if (reg.waiting && navigator.serviceWorker.controller) showUpdateBar();

                    // Sayfa açıkken bulunan güncellemeyi yakala.
                    reg.addEventListener('updatefound', function () {
                        var nw = reg.installing;
                        if (!nw) return;
                        nw.addEventListener('statechange', function () {
                            // "installed" + mevcut controller = ilk kurulum değil, gerçek güncelleme.
                            if (nw.state === 'installed' && navigator.serviceWorker.controller) showUpdateBar();
                        });
                    });
                })
                .catch(function (err) { console.warn('SW kaydı başarısız:', err); });

            // Uygulama yeniden öne gelince: güncelleme kontrolü + yüklü PWA'da bekleyen güncellemeyi tekrar göster.
            // Yüklü uygulamalar çoğu zaman tam sayfa yüklemeden "resume" olduğundan, "load" güvenilmez;
            // bu yüzden bekleyen güncelleme varsa her öne gelişte yeniden sorulur (kullanıcı önceki sefer
            // kapatsa da yeni bir yükleme şansı). Tarayıcı sekmesinde her sekme-odağında rahatsız etmemek
            // için bu tekrar-gösterim yalnız kurulu (standalone) uygulamada.
            document.addEventListener('visibilitychange', function () {
                if (document.visibilityState !== 'visible' || !swReg) return;
                swReg.update().catch(function () {});
                if (isStandalone() && swReg.waiting && navigator.serviceWorker.controller) showUpdateBar();
            });
        });
    }

    // ───────── Yükle çubuğu (beforeinstallprompt) ─────────
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
            if (window.showToast) window.showToast('success', 'Yüklendi', 'Chatural uygulaması kuruldu.');
        });
    }
})();
