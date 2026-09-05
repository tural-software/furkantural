(function () {
    'use strict';

    var ACTIONS = {
        deactivate: { label: 'Pasife Al',  variant: 'warning', done: 'kayıt pasife alındı' },
        activate:   { label: 'Aktife Al',  variant: 'success', done: 'kayıt aktife alındı' },
        restore:    { label: 'Geri Yükle', variant: 'success', done: 'kayıt geri yüklendi' },
        delete:     { label: 'Kaydı Sil',  variant: 'danger',  done: 'kayıt silindi' }
    };

    function bar() { return document.querySelector('[data-bulk-url]'); }
    function boxes() { return Array.from(document.querySelectorAll('.data-table .row-select')); }
    function selected() { return boxes().filter(function (b) { return b.checked; }); }

    function render() {
        var el = bar();
        if (!el) return;
        var picked = selected();
        var all = document.querySelector('.data-table .row-select-all');
        if (all) {
            var total = boxes().length;
            all.checked = total > 0 && picked.length === total;
            all.indeterminate = picked.length > 0 && picked.length < total;
        }
        boxes().forEach(function (b) {
            var tr = b.closest('tr');
            if (tr) tr.classList.toggle('is-selected', b.checked);
        });
        var count = el.querySelector('[data-bulk-count]');
        if (count) count.textContent = picked.length + ' seçili';
        el.hidden = picked.length === 0;
    }

    function clearSelection() {
        boxes().forEach(function (b) { b.checked = false; });
        render();
    }

    function post(el, action, ids) {
        var tokenInput = el.querySelector('input[name="__RequestVerificationToken"]');
        var data = new FormData();
        data.append('action', action);
        ids.forEach(function (id) { data.append('ids', String(id)); });
        if (tokenInput) data.append('__RequestVerificationToken', tokenInput.value);

        return fetch(el.dataset.bulkUrl, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: data
        }).then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return null; }
            if (!r.ok) {
                return r.text().then(function (body) {
                    var msg = '';
                    try { msg = JSON.parse(body).message || ''; } catch (e) { msg = ''; }
                    throw new Error(msg || 'Toplu işlem başarısız oldu.');
                });
            }
            return r.json();
        });
    }

    function run(action) {
        var el = bar();
        if (!el || !ACTIONS[action]) return;
        var ids = selected().map(function (b) { return parseInt(b.value, 10); }).filter(function (n) { return n > 0; });
        if (ids.length === 0) return;
        var meta = ACTIONS[action];

        ConfirmModal.open({
            id: ids.length + ' kayıt',
            email: 'Seçili kimlikler: ' + ids.join(', '),
            actionLabel: meta.label,
            actionVariant: meta.variant,
            onConfirm: function () {
                post(el, action, ids)
                    .then(function (result) {
                        if (!result) return;
                        var text = result.affected + ' ' + meta.done;
                        if (result.skipped && result.skipped.length > 0) {
                            text += ', ' + result.skipped.length + ' kayıt uygun durumda olmadığı için atlandı';
                        }
                        if (typeof showToast === 'function') showToast(result.affected > 0 ? 'success' : 'error', 'Toplu İşlem', text + '.');
                        clearSelection();
                        document.dispatchEvent(new CustomEvent('ft:table-reload'));
                    })
                    .catch(function (err) {
                        if (typeof showToast === 'function') showToast('error', 'Hata', err.message || 'Toplu işlem başarısız oldu.');
                    });
            }
        });
    }

    document.addEventListener('change', function (e) {
        var t = e.target;
        if (!(t instanceof HTMLInputElement)) return;
        if (t.classList.contains('row-select-all')) {
            boxes().forEach(function (b) { b.checked = t.checked; });
            render();
        } else if (t.classList.contains('row-select')) {
            render();
        }
    });

    document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('[data-bulk-action]') : null;
        if (btn) { e.preventDefault(); run(btn.dataset.bulkAction); return; }
        var clear = e.target.closest ? e.target.closest('[data-bulk-clear]') : null;
        if (clear) { e.preventDefault(); clearSelection(); }
    });

    document.addEventListener('DOMContentLoaded', render);
    document.addEventListener('ft:table-rendered', render);
})();
