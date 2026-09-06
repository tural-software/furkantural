/**
 * ConfirmModal — generic action confirmation modal
 *
 * Usage:
 *   ConfirmModal.open({ id, email, actionLabel, actionVariant, onConfirm, recordLabel, note })
 *   ConfirmModal.close()
 *
 * actionVariant: 'danger' | 'warning' | 'success' | 'neutral'
 * recordLabel:   ikinci satırın etiketi; verilmezse 'E-posta:'
 * note:          işlem satırının altına düşen tek satırlık uyarı; verilmezse hiç çizilmez
 */
(function () {
    'use strict';

    // İkonlar tek kaynaktan (window.__ICONS / IconLibrary.cs) okunur.
    var _ic = (window.__ICONS || {});
    var WARN_ICON = _ic['triangle-alert'] || '';
    var CLOSE_ICON = _ic['close'] || '';

    var _overlay = null;
    var _onConfirm = null;

    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.className = 'cm-overlay cm-overlay--hidden';
        document.body.appendChild(_overlay);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') ConfirmModal.close();
        });

        _overlay.addEventListener('click', function (e) {
            if (e.target === _overlay) ConfirmModal.close();
        });
    }

    window.ConfirmModal = {
        open: function (opts) {
            ensureOverlay();
            _onConfirm = opts.onConfirm || null;

            var variant = opts.actionVariant || 'danger';
            var esc = function (v) { return String(v == null ? '—' : v).replace(/</g, '&lt;').replace(/>/g, '&gt;'); };
            var safeEmail = esc(opts.email || '—');
            var safeLabel = esc(opts.recordLabel || 'E-posta:');
            var noteRow = opts.note
                ? '<p class="cm-note">' + esc(opts.note) + '</p>'
                : '';

            _overlay.innerHTML = '<div class="cm" role="dialog" aria-modal="true" aria-labelledby="cm-title">' +
                '<div class="cm-head">' +
                    '<span class="cm-head__icon">' + WARN_ICON + '</span>' +
                    '<div class="cm-head__text">' +
                        '<p class="cm-head__title" id="cm-title">İşlem Onayı</p>' +
                        '<p class="cm-head__sub">Bu kayıt üzerinde seçtiğiniz işlemi gerçekleştirmek istediğinize emin misiniz?</p>' +
                    '</div>' +
                    '<button class="dm-close" id="cm-close-x" aria-label="Kapat">' + CLOSE_ICON + '</button>' +
                '</div>' +
                '<div class="cm-record">' +
                    '<div class="cm-record__row"><span class="cm-record__key">Kayıt ID:</span><strong class="cm-record__val">' + (opts.id || '—') + '</strong></div>' +
                    '<div class="cm-record__row"><span class="cm-record__key">' + safeLabel + '</span><span class="cm-record__val">' + safeEmail + '</span></div>' +
                '</div>' +
                '<div class="cm-action-row">' +
                    '<span class="cm-action-label">Seçilen işlem:</span>' +
                    '<span class="dm-badge dm-badge--' + variant + '">' + (opts.actionLabel || 'İşlem') + '</span>' +
                '</div>' +
                noteRow +
                '<div class="cm-footer">' +
                    '<button class="btn-outline" id="cm-cancel">Hayır</button>' +
                    '<button class="btn-primary" id="cm-confirm">Evet, Devam Et</button>' +
                '</div>' +
            '</div>';

            _overlay.classList.remove('cm-overlay--hidden');
            document.body.style.overflow = 'hidden';

            _overlay.querySelector('#cm-close-x').addEventListener('click', ConfirmModal.close);
            _overlay.querySelector('#cm-cancel').addEventListener('click', ConfirmModal.close);
            _overlay.querySelector('#cm-confirm').addEventListener('click', function () {
                var cb = _onConfirm;
                ConfirmModal.close();
                if (cb) cb();
            });
        },

        close: function () {
            if (!_overlay) return;
            _overlay.classList.add('cm-overlay--hidden');
            _overlay.innerHTML = '';
            document.body.style.overflow = '';
            _onConfirm = null;
        }
    };
})();
