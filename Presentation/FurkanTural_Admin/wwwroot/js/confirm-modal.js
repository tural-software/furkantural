/**
 * ConfirmModal — generic action confirmation modal
 *
 * Usage:
 *   ConfirmModal.open({ id, email, actionLabel, actionVariant, onConfirm })
 *   ConfirmModal.close()
 *
 * actionVariant: 'danger' | 'warning' | 'success' | 'neutral'
 */
(function () {
    'use strict';

    var WARN_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';
    var CLOSE_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>';

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
            var safeEmail = (opts.email || '—').replace(/</g, '&lt;').replace(/>/g, '&gt;');

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
                    '<div class="cm-record__row"><span class="cm-record__key">E-posta:</span><span class="cm-record__val">' + safeEmail + '</span></div>' +
                '</div>' +
                '<div class="cm-action-row">' +
                    '<span class="cm-action-label">Seçilen işlem:</span>' +
                    '<span class="dm-badge dm-badge--' + variant + '">' + (opts.actionLabel || 'İşlem') + '</span>' +
                '</div>' +
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
