/**
 * Project Detail Modal — config + page binding
 *
 * Depends on: detail-modal.js (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 *             form-modal.js   (FormModal)
 */
(function () {
    'use strict';

    function toDateInput(val) {
        if (!val) return '';
        if (window.FtTime) return FtTime.dateInput(val);
        var d = new Date(val);
        if (isNaN(d.getTime())) return '';
        var y  = d.getFullYear();
        var mo = String(d.getMonth() + 1).padStart(2, '0');
        var dd = String(d.getDate()).padStart(2, '0');
        return y + '-' + mo + '-' + dd;
    }

    var ProjectDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen proje kaydına ait detaylar',

        header: {
            icon: 'projects',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; },
                    variant: function (r) { return r.isActive ? 'success' : 'danger'; }
                },
                {
                    label: function (r) { return r.isCompleted ? '★ Tamamlandı' : 'Devam Ediyor'; },
                    variant: function (r) { return r.isCompleted ? 'info' : 'neutral'; }
                }
            ]
        },

        sections: [
            {
                columns: 2,
                fields: [
                    {
                        label: 'Başlık',
                        icon: 'field-text',
                        value: function (r) { return r.title || '—'; }
                    },
                    {
                        label: 'Kısa Açıklama',
                        icon: 'field-text',
                        value: function (r) { return r.shortDescription || '—'; }
                    },
                    {
                        label: 'Teknolojiler',
                        icon: 'field-text',
                        value: function (r) { return r.techStack || '—'; }
                    },
                    {
                        label: 'GitHub URL',
                        icon: 'link',
                        value: function (r) { return r.gitHubUrl || '—'; }
                    },
                    {
                        label: 'Demo URL',
                        icon: 'link',
                        value: function (r) { return r.demoUrl || '—'; }
                    },
                    {
                        label: 'Tamamlandı',
                        icon: 'check-circle',
                        value: function (r) { return r.isCompleted ? 'Tamamlandı' : 'Devam Ediyor'; },
                        badgeVariant: function (r) { return r.isCompleted ? 'info' : 'neutral'; }
                    }
                ]
            },
            {
                columns: 1,
                fields: [
                    {
                        label: 'Açıklama',
                        icon: 'field-text',
                        value: function (r) { return r.description || '—'; },
                        multiline: true
                    }
                ]
            },
            DmAudit.section()
        ],

        related: { entity: 'Project' },

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            { key: 'edit',  label: 'Düzenle', icon: 'pencil', variant: 'primary', disabled: false, hidden: function(r) { return r.isDeleted || !r.isActive; } }
        ]
    };

    var FORM_FIELDS_BASE = [
        { name: 'title',            label: 'Başlık',        type: 'text',     required: false, maxLength: 500,  placeholder: 'Proje başlığını girin...' },
        { name: 'description',      label: 'Açıklama',      type: 'textarea', required: false, rows: 9,         placeholder: 'Proje açıklamasını girin...', helpText: 'Markdown desteklenir — **kalın**, *italik*, ## başlık, - liste, [bağlantı](https://…), > alıntı, `kod`.' },
        { name: 'shortDescription', label: 'Kısa Açıklama', type: 'text',     required: false, maxLength: 300,  placeholder: 'Kısa açıklama girin...' },
        { name: 'techStack',        label: 'Teknolojiler',  type: 'text',     required: false, maxLength: 500,  placeholder: 'Kullanılan teknolojileri girin...' },
        { name: 'gitHubUrl',        label: 'GitHub URL',    type: 'text',     required: false, maxLength: 1000, placeholder: 'GitHub URL girin...' },
        { name: 'demoUrl',          label: 'Demo URL',      type: 'text',     required: false, maxLength: 1000, placeholder: 'Demo URL girin...' },
        { name: 'isCompleted',      label: 'Tamamlandı',    type: 'checkbox', required: false }
    ];

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Proje Ekle',
            description: 'Yeni bir proje kaydı oluşturun.',
            submitUrl: '/Project/Create',
            submitLabel: 'Ekle',
            size: 'large',
            fields: FORM_FIELDS_BASE,
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Projeyi Düzenle',
            description: 'Mevcut proje kaydını güncelleyin.',
            submitUrl: '/Project/Update/' + id,
            submitLabel: 'Güncelle',
            size: 'large',
            fields: FORM_FIELDS_BASE,
            onSuccess: onSuccess
        };
    }

    function readRows() {
        var el = document.getElementById('__project-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__projectMeta || {};
        var params = new URLSearchParams({
            searchTitle:     meta.searchTitle     || '',
            completedFilter: meta.completedFilter || '',
            activeFilter:    meta.activeFilter    || '',
            deletedFilter:   meta.deletedFilter   || '',
            projectId:       meta.projectId       || '',
            dateFrom:        meta.dateFrom        || '',
            dateTo:          meta.dateTo          || '',
            pageNumber:      meta.pageNumber      || 1,
            pageSize:        meta.pageSize        || 10
        });

        fetch('/Project/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('project-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
        })
        .catch(function (err) {
            console.error('reloadTable hatası:', err);
        });
    }

    var ACTION_MESSAGES = {
        Delete:       { success: 'Silme işlemi başarılı.',          error: 'Silme işlemi başarısız oldu.' },
        Restore:      { success: 'Geri yükleme işlemi başarılı.',   error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null,                               error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create:       { success: 'Kayıt başarıyla oluşturuldu.',    error: 'Kayıt oluşturulamadı.' },
        Update:       { success: 'Kayıt başarıyla güncellendi.',    error: 'Kayıt güncellenemedi.' }
    };

    function resolveActionKey(actionUrl) {
        if (actionUrl.indexOf('Delete')       !== -1) return 'Delete';
        if (actionUrl.indexOf('Restore')      !== -1) return 'Restore';
        if (actionUrl.indexOf('ToggleActive') !== -1) return 'ToggleActive';
        return null;
    }

    function submitAction(form, isActive) {
        var data = new FormData(form);
        var actionKey = resolveActionKey(form.action || '');

        fetch(form.action, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: data
        })
        .then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return; }

            var msgs = ACTION_MESSAGES[actionKey] || {};

            if (r.ok) {
                var successMsg = msgs.success;
                if (actionKey === 'ToggleActive') {
                    successMsg = isActive ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.';
                }
                if (typeof showToast === 'function') {
                    showToast('success', 'Başarılı', successMsg || 'İşlem başarıyla tamamlandı.');
                }
                reloadTable();
            } else {
                return r.text().then(function (body) {
                    var serverMsg = '';
                    try { serverMsg = JSON.parse(body).message || ''; } catch (e) { serverMsg = ''; }
                    var errorMsg = serverMsg || msgs.error || 'İşlem başarısız oldu.';
                    if (typeof showToast === 'function') {
                        showToast('error', 'Hata', errorMsg);
                    }
                });
            }
        })
        .catch(function () {
            if (typeof showToast === 'function') {
                showToast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.');
            }
        });
    }

    function openEditModal(record) {
        FormModal.open(
            buildEditConfig(record.id, function () {
                if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Update.success);
                reloadTable();
            }),
            {
                title:            record.title            || '',
                description:      record.description      || '',
                shortDescription: record.shortDescription || '',
                techStack:        record.techStack        || '',
                gitHubUrl:        record.gitHubUrl        || '',
                demoUrl:          record.demoUrl          || '',
                isCompleted:      record.isCompleted      ? 'true' : ''
            }
        );
    }

    function openCreateModal() {
        FormModal.open(
            buildCreateConfig(function () {
                if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Create.success);
                reloadTable();
            }),
            {}
        );
    }

    function bindAll() {
        var rows = readRows();

        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;
                DetailModal.open(ProjectDetailConfig, record, function (key) {
                    if (key === 'edit') { DetailModal.close(); openEditModal(record); }
                });
            });
        });

        document.querySelectorAll('.ft-edit-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;
                openEditModal(record);
            });
        });

        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                var action = form.action || '';
                var actionLabel, actionVariant;
                if (action.indexOf('Delete') !== -1) {
                    actionLabel = 'Kaydı Sil'; actionVariant = 'danger';
                } else if (action.indexOf('Restore') !== -1) {
                    actionLabel = 'Geri Yükle'; actionVariant = 'success';
                } else if (action.indexOf('ToggleActive') !== -1) {
                    var isAct = record ? record.isActive : false;
                    actionLabel = isAct ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isAct ? 'warning' : 'success';
                } else {
                    actionLabel = 'İşlemi Gerçekleştir'; actionVariant = 'neutral';
                }
                var capturedIsActive = (action.indexOf('ToggleActive') !== -1) ? (record ? record.isActive : false) : null;
                var recordName = record ? (record.title || 'ID: ' + record.id) : '—';
                ConfirmModal.open({
                    id: id,
                    email: recordName,
                    actionLabel: actionLabel,
                    actionVariant: actionVariant,
                    onConfirm: function () { submitAction(form, capturedIsActive); }
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll();
        var addBtn = document.getElementById('project-add-btn');
        if (addBtn) addBtn.addEventListener('click', openCreateModal);
    });
})();
