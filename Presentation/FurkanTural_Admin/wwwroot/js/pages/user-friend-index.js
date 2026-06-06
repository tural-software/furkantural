/**
 * UserFriend Index — detail modal (read-only) + toggle/restore binding
 * Depends on: detail-modal.js, confirm-modal.js
 */
(function () {
    'use strict';

    var FriendDetailConfig = {
        title: 'Arkadaşlık Detayı',
        description: 'Seçilen arkadaşlık kaydına ait detaylar',
        header: {
            icon: 'friends',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                { label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; }, variant: function (r) { return r.isActive ? 'success' : 'danger'; } },
                { label: function (r) { return 'Silinmiş: ' + (r.isDeleted ? 'Evet' : 'Hayır'); }, variant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; } }
            ]
        },
        sections: [
            {
                columns: 2,
                fields: [
                    { label: 'Gönderen (Requester)', icon: 'user', value: function (r) { return '#' + r.requesterId; } },
                    { label: 'Alıcı (Addressee)', icon: 'user', value: function (r) { return '#' + r.addresseeId; } },
                    { label: 'Durum', icon: 'field-text', value: function (r) { return r.statusName || r.statusCode || '—'; } },
                    { label: 'Durum Kodu', icon: 'field-text', value: function (r) { return r.statusCode || '—'; } },
                    { label: 'Yanıt Tarihi', icon: 'calendar', value: function (r) { return r.respondedAt ? DmFmt.date(r.respondedAt) : '—'; } },
                    { label: 'Oluşturulma Tarihi', icon: 'calendar', value: function (r) { return DmFmt.date(r.createdAt); } },
                    { label: 'Güncellenme Tarihi', icon: 'calendar', value: function (r) { return DmFmt.date(r.updatedAt); } }
                ]
            }
        ],
        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__friend-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__friendMeta || {};
        var params = new URLSearchParams({
            statusFilter: meta.statusFilter || '', activeFilter: meta.activeFilter || '',
            deletedFilter: meta.deletedFilter || '', dateFrom: meta.dateFrom || '', dateTo: meta.dateTo || '',
            pageNumber: meta.pageNumber || 1, pageSize: meta.pageSize || 10
        });
        fetch('/UserFriend/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { if (!r.ok) throw new Error('TablePartial ' + r.status); return r.text(); })
            .then(function (html) { var s = document.getElementById('friend-table-section'); if (!s) return; s.innerHTML = html; bindAll(); })
            .catch(function (err) { console.error('reloadTable hatası:', err); });
    }

    function submitAction(form, isActive, actionKey) {
        var data = new FormData(form);
        fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: data })
            .then(function (r) {
                if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
                if (r.ok) {
                    var msg = actionKey === 'Restore' ? 'Geri yükleme işlemi başarılı.' : (isActive ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.');
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', msg);
                    reloadTable();
                } else {
                    return r.text().then(function (body) {
                        var serverMsg = ''; try { serverMsg = JSON.parse(body).message || ''; } catch (e) { }
                        if (typeof showToast === 'function') showToast('error', 'Hata', serverMsg || 'İşlem başarısız oldu.');
                    });
                }
            })
            .catch(function () { if (typeof showToast === 'function') showToast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.'); });
    }

    function bindAll() {
        var rows = readRows();
        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                if (rec) DetailModal.open(FriendDetailConfig, rec, function () { });
            });
        });
        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                var action = form.action || '', actionLabel, actionVariant, actionKey;
                if (action.indexOf('Restore') !== -1) { actionKey = 'Restore'; actionLabel = 'Geri Yükle'; actionVariant = 'success'; }
                else { actionKey = 'ToggleActive'; var a = rec ? rec.isActive : false; actionLabel = a ? 'Pasife Al' : 'Aktife Al'; actionVariant = a ? 'warning' : 'success'; }
                var capturedIsActive = rec ? rec.isActive : false;
                var recordName = rec ? ('#' + rec.requesterId + ' → #' + rec.addresseeId) : ('ID: ' + id);
                ConfirmModal.open({ id: id, email: recordName, actionLabel: actionLabel, actionVariant: actionVariant, onConfirm: function () { submitAction(form, capturedIsActive, actionKey); } });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () { bindAll(); });
})();
