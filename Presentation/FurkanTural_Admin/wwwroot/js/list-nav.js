(function () {
    'use strict';

    var section = null;
    var controller = '';
    var metaName = '';
    var listPath = '';
    var params = null;
    var seq = 0;
    var inflight = null;
    var live = null;
    var statsStamp = 0;
    var appliedStamp = 0;

    function normalizePath(value) {
        var path = (value || '').replace(/\/+$/, '');
        path = path.replace(/\/index$/i, '');
        return path.toLowerCase();
    }

    function sameList(href) {
        var url;
        try { url = new URL(href, window.location.href); } catch (e) { return false; }
        if (url.origin !== window.location.origin) return false;
        return normalizePath(url.pathname) === listPath;
    }

    function fromForm(form) {
        var next = new URLSearchParams();
        new FormData(form).forEach(function (value, key) {
            if (typeof value !== 'string') return;
            if (value === '') return;
            next.append(key, value);
        });
        return next;
    }

    function ensureLive() {
        live = document.createElement('p');
        live.className = 'ft-list-live';
        live.setAttribute('aria-live', 'polite');
        document.body.appendChild(live);
    }

    function speak() {
        if (!live) return;
        var range = section.querySelector('.tbl-footer__range');
        live.textContent = range ? (range.textContent || '').trim() : '';
    }

    function maybeScroll() {
        var card = section.closest ? (section.closest('.card') || section) : section;
        if (card.getBoundingClientRect().top < 0) {
            card.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function syncMeta() {
        if (!metaName) return;
        var meta = window[metaName];
        if (!meta) return;

        Object.keys(meta).forEach(function (key) {
            var value = params.get(key);
            if (key === 'pageNumber') {
                meta[key] = parseInt(value || '1', 10) || 1;
            } else if (key === 'pageSize') {
                meta[key] = parseInt(value || meta[key] || '10', 10) || 10;
            } else {
                meta[key] = value === null ? '' : value;
            }
        });
    }

    function readStats(root) {
        var holder = root.querySelector('#__list-stats-json');
        if (!holder) return null;

        try { return JSON.parse(holder.textContent || '{}'); } catch (e) { return null; }
    }

    function applyStats(stats, stamp) {
        if (!stats || stamp < appliedStamp) return;
        appliedStamp = stamp;

        Object.keys(stats).forEach(function (key) {
            document.querySelectorAll('[data-stat="' + key + '"]').forEach(function (node) {
                node.textContent = stats[key];
            });
            document.querySelectorAll('[data-stat-when="' + key + '"]').forEach(function (node) {
                node.hidden = !sifirdanBuyuk(stats[key]);
            });
        });
    }

    function sifirdanBuyuk(value) {
        return /[1-9]/.test(String(value === null || value === undefined ? '' : value));
    }

    function settle(announce) {
        document.dispatchEvent(new CustomEvent('ft:table-rendered'));
        if (window.MobileList && typeof window.MobileList.stamp === 'function') {
            window.MobileList.stamp();
        }
        if (announce) {
            speak();
            maybeScroll();
        }
    }

    function load(announce) {
        if (!section) return;

        var mine = ++seq;
        var stamp = ++statsStamp;
        if (inflight) inflight.abort();
        inflight = ('AbortController' in window) ? new AbortController() : null;

        section.setAttribute('aria-busy', 'true');
        section.classList.add('tbl-loading');

        var options = { headers: { 'X-Requested-With': 'XMLHttpRequest' } };
        if (inflight) options.signal = inflight.signal;

        fetch('/' + controller + '/TablePartial?' + params.toString(), options)
            .then(function (response) {
                if (response.status === 401) {
                    window.location.href = '/Auth/Login';
                    return null;
                }
                if (!response.ok) throw new Error('TablePartial ' + response.status);
                return response.text();
            })
            .then(function (html) {
                if (html === null || mine !== seq) return;
                section.innerHTML = html;
                syncMeta();
                applyStats(readStats(section), stamp);
                settle(announce);
            })
            .catch(function (error) {
                if (error && error.name === 'AbortError') return;
                if (mine !== seq) return;
                if (typeof showToast === 'function') {
                    showToast('error', 'Hata', 'Liste yenilenemedi. Lütfen tekrar deneyin.');
                }
            })
            .then(function () {
                if (mine !== seq) return;
                section.removeAttribute('aria-busy');
                section.classList.remove('tbl-loading');
            });
    }

    function refreshStats() {
        if (!section) return;

        var stamp = ++statsStamp;

        fetch('/' + controller + '/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (response) {
                if (response.status === 401) {
                    window.location.href = '/Auth/Login';
                    return null;
                }
                return response.ok ? response.text() : null;
            })
            .then(function (html) {
                if (html === null) return;
                var parsed = document.createElement('template');
                parsed.innerHTML = html;
                applyStats(readStats(parsed.content), stamp);
            })
            .catch(function () { });
    }

    function go(next) {
        params = next;
        var query = params.toString();
        window.history.pushState({ ftList: query }, '', window.location.pathname + (query ? '?' + query : ''));
        load(true);
    }

    function onSubmit(event) {
        var form = event.target;
        if (!form || form.tagName !== 'FORM') return;
        if ((form.getAttribute('method') || 'get').toLowerCase() !== 'get') return;
        if (!sameList(form.action)) return;

        event.preventDefault();
        go(fromForm(form));
    }

    function onClick(event) {
        if (event.defaultPrevented) return;
        if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

        var anchor = event.target && event.target.closest ? event.target.closest('a[href]') : null;
        if (!anchor) return;
        if (anchor.hasAttribute('download')) return;
        if (anchor.target && anchor.target !== '' && anchor.target !== '_self') return;
        if (!sameList(anchor.href)) return;

        event.preventDefault();
        if (anchor.classList.contains('pag-btn--off')) return;

        go(new URLSearchParams(new URL(anchor.href, window.location.href).search));
    }

    function onPopState(event) {
        if (!event.state || typeof event.state.ftList !== 'string') return;
        params = new URLSearchParams(event.state.ftList);
        load(true);
    }

    function init() {
        section = document.querySelector('[data-list-controller]');
        if (!section) return;

        controller = section.dataset.listController || '';
        metaName = section.dataset.listMeta || '';
        if (!controller) { section = null; return; }

        listPath = normalizePath(window.location.pathname);
        params = new URLSearchParams(window.location.search);

        ensureLive();
        window.history.replaceState({ ftList: params.toString() }, '', window.location.href);

        document.addEventListener('submit', onSubmit);
        document.addEventListener('click', onClick);
        window.addEventListener('popstate', onPopState);
        document.addEventListener('ft:table-reload', function () { load(false); });
    }

    window.FtList = {
        reload: function () { load(false); },
        refreshStats: refreshStats,
        go: function (next) {
            if (!section) return;
            go(next instanceof URLSearchParams ? next : new URLSearchParams(next || ''));
        },
        params: function () { return new URLSearchParams(params ? params.toString() : ''); }
    };

    document.addEventListener('change', function (event) {
        var select = event.target;
        if (!select || !select.classList || !select.classList.contains('page-size-sel') || !select.form) return;

        if (typeof select.form.requestSubmit === 'function') {
            select.form.requestSubmit();
        } else {
            select.form.submit();
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
