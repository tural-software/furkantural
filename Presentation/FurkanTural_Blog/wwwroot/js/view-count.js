(function () {
    'use strict';

    var DWELL_MS = 5000;
    var KEY_PREFIX = 'ft:okuma:';

    var script = document.currentScript;
    if (!script) return;

    var postId = script.dataset.postId;
    var url = script.dataset.viewUrl;
    if (!postId || !url) return;

    function today() {
        var d = new Date();
        return d.getFullYear() + '-' + (d.getMonth() + 1) + '-' + d.getDate();
    }

    function alreadyCounted() {
        try {
            return localStorage.getItem(KEY_PREFIX + postId) === today();
        } catch (e) {
            return false;
        }
    }

    function remember() {
        try {
            localStorage.setItem(KEY_PREFIX + postId, today());
        } catch (e) {
        }
    }

    function token() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function count() {
        if (alreadyCounted()) return;

        var verification = token();
        if (!verification) return;

        remember();

        fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': verification },
            keepalive: true
        }).catch(function () { });
    }

    var timer = null;

    function start() {
        if (timer !== null || document.hidden) return;
        timer = window.setTimeout(count, DWELL_MS);
    }

    function stop() {
        if (timer === null) return;
        window.clearTimeout(timer);
        timer = null;
    }

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) stop(); else start();
    });

    start();
})();
