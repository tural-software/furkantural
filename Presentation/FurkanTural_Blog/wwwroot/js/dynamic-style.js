(function () {
    'use strict';

    var COLOR = /^(#[0-9a-fA-F]{3,8}|(rgb|hsl)a?\([0-9.,%\s]+\))$/;
    var VARIABLES = { '--cat-color': true, '--chip-color': true };
    var SELECTOR = '[data-style-width],[data-style-bg],[data-css-var],[data-bg-image]';

    function apply(el) {
        var width = el.getAttribute('data-style-width');
        if (width !== null && width !== '' && isFinite(Number(width))) {
            el.style.width = Math.max(0, Math.min(100, Number(width))) + '%';
        }

        var background = el.getAttribute('data-style-bg');
        if (background !== null && COLOR.test(background)) {
            el.style.background = background;
        }

        var name = el.getAttribute('data-css-var');
        var value = el.getAttribute('data-css-value');
        if (name && value !== null && VARIABLES[name] === true && COLOR.test(value)) {
            el.style.setProperty(name, value);
        }

        var image = el.getAttribute('data-bg-image');
        if (image) {
            var url = null;
            try { url = new URL(image, window.location.href); } catch (e) { url = null; }
            if (url && (url.protocol === 'https:' || url.protocol === 'http:')) {
                el.style.backgroundImage = 'url(' + JSON.stringify(url.href) + ')';
            }
        }
    }

    function scan(node) {
        if (!node || node.nodeType !== 1) return;
        if (node.matches(SELECTOR)) apply(node);
        node.querySelectorAll(SELECTOR).forEach(apply);
    }

    function start() {
        scan(document.documentElement);
        new MutationObserver(function (records) {
            records.forEach(function (record) {
                record.addedNodes.forEach(scan);
            });
        }).observe(document.documentElement, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
