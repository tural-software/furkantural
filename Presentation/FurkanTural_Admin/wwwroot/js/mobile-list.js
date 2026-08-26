/**
 * MobileList — dar ekranda liste ve süzgeç davranışı
 *
 * Yirmi bir modülün tablosu aynı iskeleti paylaşıyor; kart görünümü için markup
 * çoğaltmak yerine hücre etiketleri tablonun KENDİ başlık satırından türetilir.
 * Böylece yeni bir kolon eklendiğinde kart görünümü kendiliğinden doğru kalır.
 *
 * 1. Her hücreye kolon adı (data-label) ve rolü (kimlik / başlık / işlem) yazılır.
 * 2. Süzgeç kartı dar ekranda alttan açılan sayfaya dönüşür; etkin süzgeçler
 *    liste üstünde çip olarak görünür.
 */
(function () {
    'use strict';

    function stampTable(table) {
        var headers = Array.prototype.map.call(table.querySelectorAll('thead th'), function (th) {
            return { text: (th.textContent || '').trim(), cls: th.className || '' };
        });
        if (headers.length === 0) return;

        Array.prototype.forEach.call(table.querySelectorAll('tbody tr'), function (tr) {
            if (tr.classList.contains('empty-row')) return;

            var cells = tr.children;
            if (cells.length !== headers.length) return;

            var titleTaken = false;
            for (var i = 0; i < cells.length; i++) {
                var td = cells[i];
                var h = headers[i];
                td.setAttribute('data-label', h.text);

                if (h.cls.indexOf('col-id') !== -1) {
                    td.classList.add('cell-id');
                } else if (h.cls.indexOf('col-actions') !== -1) {
                    td.classList.add('cell-actions');
                } else if (!titleTaken) {
                    td.classList.add('cell-title');
                    titleTaken = true;
                }
            }
        });
    }

    function controlsOf(bar) {
        return Array.prototype.filter.call(
            bar.querySelectorAll('input, select'),
            function (el) {
                if (el.type === 'hidden') return false;
                if (el.classList.contains('fbs-search')) return false;   // görünen arama alanı; değeri gizli alanda
                return true;
            });
    }

    function activeFilters(bar) {
        var out = [];
        controlsOf(bar).forEach(function (el) {
            var value = (el.value || '').trim();
            if (value === '') return;

            var group = el.closest('.filter-group');
            var label = group ? group.querySelector('label') : null;
            var shown = value;

            if (el.tagName === 'SELECT') {
                var opt = el.options[el.selectedIndex];
                shown = opt ? opt.textContent.trim() : value;
            }

            out.push({
                label: label ? label.textContent.trim() : '',
                value: shown
            });
        });
        return out;
    }

    function buildFilterSheet(bar) {
        var card = bar.closest('.card');
        if (!card || card.dataset.filterCard === '1') return;
        card.dataset.filterCard = '1';

        var active = activeFilters(bar);

        var trigger = document.createElement('div');
        trigger.className = 'filter-trigger';

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'filter-trigger__btn';
        button.setAttribute('aria-expanded', 'false');
        button.innerHTML = (window.__ICONS && window.__ICONS['filter-icon'] ? window.__ICONS['filter-icon'] : '')
            + '<span>Filtreler</span>'
            + (active.length ? '<span class="filter-trigger__count">' + active.length + '</span>' : '');
        trigger.appendChild(button);

        if (active.length) {
            var chips = document.createElement('div');
            chips.className = 'filter-chips';
            active.forEach(function (f) {
                var chip = document.createElement('span');
                chip.className = 'filter-chip';
                chip.textContent = f.label ? f.label + ': ' + f.value : f.value;
                chips.appendChild(chip);
            });
            trigger.appendChild(chips);
        }

        card.parentNode.insertBefore(trigger, card);

        // Zemin, kartla AYNI ebeveyne konur: .app kendi yigin baglami oldugu icin
        // body'ye eklenen bir zemin, sayfanin z-index'i ne olursa olsun ustunde kalirdi.
        var scrim = document.createElement('div');
        scrim.className = 'filter-scrim';
        card.parentNode.insertBefore(scrim, card);

        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'filter-sheet__close';
        close.setAttribute('aria-label', 'Süzgeçleri kapat');
        close.innerHTML = (window.__ICONS && window.__ICONS['x-mark'] ? window.__ICONS['x-mark'] : '×');
        card.insertBefore(close, card.firstChild);

        function open() {
            document.body.classList.add('filter-open');
            button.setAttribute('aria-expanded', 'true');
        }
        function shut() {
            document.body.classList.remove('filter-open');
            button.setAttribute('aria-expanded', 'false');
            button.focus();
        }

        button.addEventListener('click', open);
        close.addEventListener('click', shut);
        scrim.addEventListener('click', shut);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && document.body.classList.contains('filter-open')) shut();
        });
    }

    function init() {
        Array.prototype.forEach.call(document.querySelectorAll('.data-table'), stampTable);

        var bar = document.querySelector('.filter-bar');
        if (bar) buildFilterSheet(bar);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Tablo sunucudan yeniden çizildiğinde (sayfa listeleri kendi tablosunu tazeliyor)
    // etiketlerin yeniden yazılabilmesi için dışarıya açılır.
    window.MobileList = { stamp: init };
})();
