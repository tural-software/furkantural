/* Uygulama içi onay ve metin penceresi — yerel confirm/prompt yerine kullanılır. */
(function () {
    'use strict';

    var overlay = document.getElementById('askDialog');
    if (!overlay) return;

    var card = overlay.querySelector('.ask-card');
    var titleEl = document.getElementById('askTitle');
    var textEl = document.getElementById('askText');
    var input = document.getElementById('askInput');
    var okBtn = document.getElementById('askOk');
    var cancelBtn = document.getElementById('askCancel');

    var settle = null;
    var asksText = false;
    var lastFocus = null;

    function finish(value) {
        if (!settle) return;
        var done = settle;
        settle = null;
        overlay.hidden = true;
        document.removeEventListener('keydown', onKey, true);
        if (lastFocus && lastFocus.isConnected) { try { lastFocus.focus(); } catch (e) { } }
        done(value);
    }

    function submit() { finish(asksText ? input.value : true); }
    function cancel() { finish(asksText ? null : false); }

    function focusables() {
        return Array.prototype.filter.call(card.querySelectorAll('button, input'), function (el) {
            return !el.hidden && !el.disabled;
        });
    }

    // Odak pencerenin içinde döner; Escape vazgeçer. Yerel confirm'in engellediği şeyi engellemeden.
    function onKey(e) {
        if (!settle) return;
        if (e.key === 'Escape') { e.preventDefault(); cancel(); return; }
        if (e.key === 'Enter' && document.activeElement !== cancelBtn) { e.preventDefault(); submit(); return; }
        if (e.key !== 'Tab') return;
        var items = focusables();
        if (!items.length) return;
        var first = items[0];
        var last = items[items.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    okBtn.addEventListener('click', submit);
    cancelBtn.addEventListener('click', cancel);
    overlay.addEventListener('mousedown', function (e) { if (e.target === overlay) cancel(); });

    function open(opts) {
        finish(asksText ? null : false);

        asksText = !!opts.asksText;
        lastFocus = document.activeElement;

        titleEl.textContent = opts.title || '';
        textEl.textContent = opts.text || '';
        textEl.hidden = !opts.text;
        okBtn.textContent = opts.okText || (asksText ? 'Kaydet' : 'Tamam');
        okBtn.className = 'ask-btn ' + (opts.danger ? 'btn-danger' : 'btn-primary');
        cancelBtn.textContent = opts.cancelText || 'Vazgeç';

        input.hidden = !asksText;
        input.value = asksText ? (opts.value || '') : '';

        overlay.hidden = false;
        document.addEventListener('keydown', onKey, true);

        return new Promise(function (resolve) {
            settle = resolve;
            if (asksText) { input.focus(); input.select(); }
            else okBtn.focus();
        });
    }

    window.askConfirm = function (opts) { return open(opts || {}); };
    window.askPrompt = function (opts) {
        var o = opts || {};
        o.asksText = true;
        return open(o);
    };
})();
