/**
 * Filter Blog Select — filter bar searchable dropdown component
 *
 * Auto-initialises every .fbs-wrap[data-fbs-endpoint] element on DOMContentLoaded.
 *
 * Required HTML structure inside .fbs-wrap:
 *   <input class="fbs-search filter-input" type="text" autocomplete="off" placeholder="..." />
 *   <input class="fbs-value"               type="hidden" name="blogId" value="" />
 *   <div  class="fbs-dropdown"></div>
 *
 * data-attributes on .fbs-wrap:
 *   data-fbs-endpoint  — URL to fetch [{value, label}] JSON
 *   data-fbs-initial   — pre-selected blog id (optional, from server)
 */
(function () {
    'use strict';

    /* options cache keyed by endpoint */
    var _cache = {};

    function fetchOptions(endpoint, cb) {
        if (_cache[endpoint]) { cb(_cache[endpoint]); return; }
        fetch(endpoint, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) { _cache[endpoint] = data || []; cb(_cache[endpoint]); })
            .catch(function () { _cache[endpoint] = []; cb([]); });
    }

    function escHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function initWrap(wrap) {
        var endpoint  = wrap.dataset.fbsEndpoint;
        var initialId = (wrap.dataset.fbsInitial || '').trim();

        var searchInput = wrap.querySelector('.fbs-search');
        var valueInput  = wrap.querySelector('.fbs-value');
        var dropdown    = wrap.querySelector('.fbs-dropdown');

        if (!searchInput || !valueInput || !dropdown) return;

        var allOptions   = [];
        var selectedValue = '';
        var selectedLabel = '';

        /* ── helpers ── */

        function openDropdown() { dropdown.classList.add('fbs-open'); }
        function closeDropdown() { dropdown.classList.remove('fbs-open'); }

        function renderHint(msg) {
            dropdown.innerHTML = '<div class="fbs-hint">' + escHtml(msg) + '</div>';
        }

        function renderOptions(list) {
            if (list.length === 0) {
                renderHint('Sonuç bulunamadı');
                return;
            }
            var html = '';
            for (var i = 0; i < list.length; i++) {
                var opt = list[i];
                var active = String(opt.value) === selectedValue ? ' fbs-option--active' : '';
                html += '<div class="fbs-option' + active + '" data-value="' + escHtml(opt.value) + '" data-label="' + escHtml(opt.label) + '">'
                      + escHtml(opt.label)
                      + '</div>';
            }
            dropdown.innerHTML = html;

            /* bind clicks */
            dropdown.querySelectorAll('.fbs-option').forEach(function (el) {
                el.addEventListener('mousedown', function (e) {
                    /* mousedown fires before blur — prevent blur from closing first */
                    e.preventDefault();
                    selectOption(el.dataset.value, el.dataset.label);
                });
            });
        }

        function filterAndRender(query) {
            var q = query.trim().toLowerCase();
            if (q.length === 0) {
                /* Only show hint when nothing is currently selected */
                if (!selectedValue) {
                    renderHint('En az 1 karakter girin');
                } else {
                    /* selected blog exists — show all options so user can see/change */
                    renderOptions(allOptions);
                }
                return;
            }
            var matched = allOptions.filter(function (o) {
                return o.label.toLowerCase().indexOf(q) !== -1;
            });
            renderOptions(matched);
        }

        function selectOption(value, label) {
            selectedValue = String(value);
            selectedLabel = label;
            valueInput.value  = selectedValue;
            searchInput.value = selectedLabel;
            closeDropdown();
        }

        function clearSelection() {
            selectedValue = '';
            selectedLabel = '';
            valueInput.value  = '';
        }

        /* ── events ── */

        searchInput.addEventListener('focus', function () {
            filterAndRender(searchInput.value);
            openDropdown();
        });

        searchInput.addEventListener('input', function () {
            var text = searchInput.value;

            /* if user cleared the text, also clear the hidden selection */
            if (text.length === 0 && selectedValue) {
                clearSelection();
            }

            filterAndRender(text);
            openDropdown();
        });

        searchInput.addEventListener('blur', function () {
            /* Short delay lets mousedown on an option fire first */
            setTimeout(function () {
                closeDropdown();
                /* Reset text to the selected label (or clear if nothing selected) */
                if (selectedValue) {
                    searchInput.value = selectedLabel;
                } else {
                    searchInput.value = '';
                }
            }, 150);
        });

        /* close on outside click */
        document.addEventListener('click', function (e) {
            if (!wrap.contains(e.target)) {
                closeDropdown();
            }
        });

        /* ── init with fetched options ── */

        fetchOptions(endpoint, function (options) {
            allOptions = options;

            if (initialId) {
                /* pre-select: find matching option and show its label */
                for (var i = 0; i < options.length; i++) {
                    if (String(options[i].value) === initialId) {
                        selectOption(options[i].value, options[i].label);
                        break;
                    }
                }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.fbs-wrap[data-fbs-endpoint]').forEach(initWrap);
    });
})();
