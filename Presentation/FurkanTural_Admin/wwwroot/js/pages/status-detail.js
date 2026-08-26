/**
 * Status Detail Modal — config + page binding (Role deseni)
 * Depends on: detail-modal.js, confirm-modal.js, form-modal.js
 */
(function () {
    'use strict';

    var StatusDetailConfig = {
        title: 'Durum Detayı',
        description: 'Seçilen statü kaydına ait detaylar',
        header: {
            icon: 'statuses',
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
                    { label: 'Grup', icon: 'field-text', value: function (r) { return r.group || '—'; } },
                    { label: 'Kod', icon: 'field-text', value: function (r) { return r.code || '—'; } },
                    { label: 'Ad', icon: 'field-text', value: function (r) { return r.name || '—'; } },
                    { label: 'Renk', icon: 'field-text', value: function (r) { return r.color || '—'; } },
                    { label: 'Sıra', icon: 'field-text', value: function (r) { return (r.sortOrder != null ? r.sortOrder : '—'); } },
                    { label: 'Açıklama', icon: 'field-text', value: function (r) { return r.description || '—'; } }
                ]
            },
            DmAudit.section()
        ],
        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            { key: 'edit', label: 'Düzenle', icon: 'pencil', variant: 'primary', hidden: function (r) { return r.isDeleted || !r.isActive; } }
        ]
    };

    var STATUS_FORM_FIELDS = [
        { name: 'group', label: 'Grup', type: 'text', required: true, maxLength: 80, placeholder: 'Örn: Friendship' },
        { name: 'code', label: 'Kod', type: 'text', required: true, maxLength: 80, placeholder: 'Örn: Pending' },
        { name: 'name', label: 'Ad', type: 'text', required: false, maxLength: 150, placeholder: 'Örn: Beklemede' },
        { name: 'description', label: 'Açıklama', type: 'text', required: false, maxLength: 500, placeholder: 'Açıklama...' },
        { name: 'color', label: 'Renk (hex)', type: 'text', required: false, maxLength: 32, placeholder: '#f59e0b' },
        { name: 'sortOrder', label: 'Sıra', type: 'text', required: false, placeholder: '0' }
    ];

    function buildCreateConfig(onSuccess) {
        return { title: 'Yeni Durum Ekle', description: 'Yeni bir statü kaydı oluşturun.', submitUrl: '/Status/Create', submitLabel: 'Ekle', fields: STATUS_FORM_FIELDS, onSuccess: onSuccess };
    }
    function buildEditConfig(id, onSuccess) {
        return { title: 'Durum Düzenle', description: 'Mevcut statü kaydını güncelleyin.', submitUrl: '/Status/Update/' + id, submitLabel: 'Güncelle', fields: STATUS_FORM_FIELDS, onSuccess: onSuccess };
    }

    function readRows() {
        var el = document.getElementById('__status-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__statusMeta || {};
        var params = new URLSearchParams({
            name: meta.name || '', groupFilter: meta.groupFilter || '',
            activeFilter: meta.activeFilter || '', deletedFilter: meta.deletedFilter || '',
            dateFrom: meta.dateFrom || '', dateTo: meta.dateTo || '',
            pageNumber: meta.pageNumber || 1, pageSize: meta.pageSize || 10
        });
        fetch('/Status/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { if (!r.ok) throw new Error('TablePartial ' + r.status); return r.text(); })
            .then(function (html) { var s = document.getElementById('status-table-section'); if (!s) return; s.innerHTML = html; bindAll(); })
            .catch(function (err) { console.error('reloadTable hatası:', err); });
    }

    var ACTION_MESSAGES = {
        Delete: { success: 'Silme işlemi başarılı.', error: 'Silme işlemi başarısız oldu.' },
        Restore: { success: 'Geri yükleme işlemi başarılı.', error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null, error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create: { success: 'Kayıt başarıyla oluşturuldu.', error: 'Kayıt oluşturulamadı.' },
        Update: { success: 'Kayıt başarıyla güncellendi.', error: 'Kayıt güncellenemedi.' }
    };

    function resolveActionKey(u) {
        if (u.indexOf('Delete') !== -1) return 'Delete';
        if (u.indexOf('Restore') !== -1) return 'Restore';
        if (u.indexOf('ToggleActive') !== -1) return 'ToggleActive';
        return null;
    }

    function submitAction(form, isActive) {
        var data = new FormData(form);
        var actionKey = resolveActionKey(form.action || '');
        fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: data })
            .then(function (r) {
                if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
                var msgs = ACTION_MESSAGES[actionKey] || {};
                if (r.ok) {
                    var successMsg = msgs.success;
                    if (actionKey === 'ToggleActive') successMsg = isActive ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.';
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', successMsg || 'İşlem tamamlandı.');
                    reloadTable();
                } else {
                    return r.text().then(function (body) {
                        var serverMsg = ''; try { serverMsg = JSON.parse(body).message || ''; } catch (e) { }
                        if (typeof showToast === 'function') showToast('error', 'Hata', serverMsg || msgs.error || 'İşlem başarısız oldu.');
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
                if (!rec) return;
                DetailModal.open(StatusDetailConfig, rec, function (key) { if (key === 'edit') { DetailModal.close(); openEditModal(rec); } });
            });
        });
        document.querySelectorAll('.ft-edit-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                if (rec) openEditModal(rec);
            });
        });
        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                var action = form.action || '', actionLabel, actionVariant;
                if (action.indexOf('Delete') !== -1) { actionLabel = 'Kaydı Sil'; actionVariant = 'danger'; }
                else if (action.indexOf('Restore') !== -1) { actionLabel = 'Geri Yükle'; actionVariant = 'success'; }
                else if (action.indexOf('ToggleActive') !== -1) { var a = rec ? rec.isActive : false; actionLabel = a ? 'Pasife Al' : 'Aktife Al'; actionVariant = a ? 'warning' : 'success'; }
                else { actionLabel = 'İşlemi Gerçekleştir'; actionVariant = 'neutral'; }
                var capturedIsActive = (action.indexOf('ToggleActive') !== -1) ? (rec ? rec.isActive : false) : null;
                var recordName = rec ? (rec.name || rec.code || 'ID: ' + rec.id) : '—';
                ConfirmModal.open({ id: id, email: recordName, actionLabel: actionLabel, actionVariant: actionVariant, onConfirm: function () { submitAction(form, capturedIsActive); } });
            });
        });
    }

    function openCreateModal() {
        FormModal.open(buildCreateConfig(function () { if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Create.success); reloadTable(); }), { sortOrder: '0' });
    }
    function openEditModal(rec) {
        FormModal.open(buildEditConfig(rec.id, function () { if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Update.success); reloadTable(); }), {
            group: rec.group || '', code: rec.code || '', name: rec.name || '', description: rec.description || '', color: rec.color || '', sortOrder: (rec.sortOrder != null ? String(rec.sortOrder) : '0')
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll();
        var addBtn = document.getElementById('status-add-btn');
        if (addBtn) addBtn.addEventListener('click', openCreateModal);
    });
})();
