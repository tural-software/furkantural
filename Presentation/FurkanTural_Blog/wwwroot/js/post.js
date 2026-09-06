(function () {
    'use strict';

    var bar = document.querySelector('[data-read-progress]');
    var article = document.querySelector('[data-read-target]');

    if (bar && article) {
        var ticking = false;

        function update() {
            ticking = false;
            var rect = article.getBoundingClientRect();
            var viewport = window.innerHeight || document.documentElement.clientHeight;
            var total = rect.height - viewport;
            var pct = total <= 0 ? 100 : ((-rect.top) / total) * 100;
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            bar.style.width = pct.toFixed(1) + '%';
            bar.parentNode.setAttribute('aria-valuenow', Math.round(pct));
        }

        function onScroll() {
            if (ticking) return;
            ticking = true;
            window.requestAnimationFrame(update);
        }

        window.addEventListener('scroll', onScroll, { passive: true });
        window.addEventListener('resize', onScroll, { passive: true });
        update();
    }

    var shareBtn = document.querySelector('[data-share]');
    if (shareBtn && navigator.share) {
        shareBtn.hidden = false;
        shareBtn.addEventListener('click', function () {
            navigator.share({ title: document.title, url: location.href }).catch(function () {});
        });
    }

    var copyBtn = document.querySelector('[data-copy-link]');
    if (copyBtn) {
        copyBtn.addEventListener('click', function () {
            var done = function () {
                var old = copyBtn.getAttribute('data-label') || copyBtn.textContent;
                copyBtn.setAttribute('data-label', old);
                copyBtn.textContent = 'Kopyalandı';
                window.setTimeout(function () { copyBtn.textContent = old; }, 2000);
            };
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(location.href).then(done).catch(function () {});
            }
        });
    }
})();
