/**
 * Report Index — detail modal + status update (inline select) + toggle/restore
 * Depends on: detail-modal.js, confirm-modal.js
 */
(function () {
    'use strict';

    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }

    var statusVariant = { pending: 'warning', reviewed: 'neutral', dismissed: 'neutral', actiontaken: 'success' };

    var ReportDetailConfig = {
        title: 'Şikayet Detayı',
        description: 'Seçilen şikayet kaydına ait detaylar',
        header: {
            icon: 'reports',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                { label: function (r) { return r.status || '—'; }, variant: function (r) { return statusVariant[String(r.status || '').toLowerCase()] || 'neutral'; } },
                { label: function (r) { return r.targetType || '—'; }, variant: function () { return 'neutral'; } },
                { label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; }, variant: function (r) { return r.isActive ? 'success' : 'danger'; } },
                { label: function (r) { return 'Silinmiş: ' + (r.isDeleted ? 'Evet' : 'Hayır'); }, variant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; } }
            ]
        },
        sections: [
            {
                columns: 2,
                fields: [
                    { label: 'Şikayetçi', icon: 'user', value: function (r) { return (r.reporterName || '#' + r.reporterId); } },
                    { label: 'Hedef Kullanıcı', icon: 'user', value: function (r) { return r.reportedUserName || (r.reportedUserId ? '#' + r.reportedUserId : '—'); } },
                    { label: 'Tür', icon: 'field-text', value: function (r) { return r.targetType || '—'; } },
                    { label: 'Hedef Id', icon: 'field-text', value: function (r) { return r.targetId != null ? r.targetId : '—'; } },
                    { label: 'Durum', icon: 'field-text', value: function (r) { return r.status || '—'; } },
                    { label: 'Oluşturulma Tarihi', icon: 'calendar', value: function (r) { return DmFmt.date(r.createdAt); } }
                ]
            },
            {
                title: 'Açıklama',
                icon: 'field-text',
                columns: 1,
                fields: [
                    { label: 'Neden', icon: 'field-text', html: true, value: function (r) { return r.reason ? esc(r.reason) : '—'; } },
                    { label: 'Yönetici Notu', icon: 'field-text', html: true, value: function (r) { return r.adminNote ? esc(r.adminNote) : '—'; } }
                ]
            }
        ],
        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__report-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__reportMeta || {};
        var params = new URLSearchParams({
            search: meta.search || '', typeFilter: meta.typeFilter || '', statusFilter: meta.statusFilter || '',
            deletedFilter: meta.deletedFilter || '', dateFrom: meta.dateFrom || '', dateTo: meta.dateTo || '',
            pageNumber: meta.pageNumber || 1, pageSize: meta.pageSize || 10
        });
        fetch('/Report/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { if (!r.ok) throw new Error('TablePartial ' + r.status); return r.text(); })
            .then(function (html) { var s = document.getElementById('report-table-section'); if (!s) return; s.innerHTML = html; bindAll(); })
            .catch(function (err) { console.error('reloadTable hatası:', err); });
    }

    function postForm(form, okMsg) {
        var data = new FormData(form);
        fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: data })
            .then(function (r) {
                if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
                if (r.ok) {
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', okMsg);
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
                if (rec) DetailModal.open(ReportDetailConfig, rec, function () { });
            });
        });

        // Durum değiştir (inline select → AJAX)
        document.querySelectorAll('.status-sel').forEach(function (sel) {
            sel.addEventListener('change', function () {
                postForm(sel.closest('form'), 'Şikayet durumu güncellendi.');
            });
        });

        // Aktif/Pasif + Geri Yükle
        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                var action = form.action || '', actionLabel, actionVariant, okMsg;
                if (action.indexOf('Restore') !== -1) { actionLabel = 'Geri Yükle'; actionVariant = 'success'; okMsg = 'Geri yükleme işlemi başarılı.'; }
                else { var a = rec ? rec.isActive : false; actionLabel = a ? 'Pasife Al' : 'Aktife Al'; actionVariant = a ? 'warning' : 'success'; okMsg = a ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.'; }
                var recordName = rec ? ('#' + rec.reporterId + ' → ' + (rec.reportedUserName || (rec.reportedUserId ? '#' + rec.reportedUserId : rec.targetType))) : ('ID: ' + id);
                ConfirmModal.open({ id: id, email: recordName, actionLabel: actionLabel, actionVariant: actionVariant, onConfirm: function () { postForm(form, okMsg); } });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () { bindAll(); });
})();
