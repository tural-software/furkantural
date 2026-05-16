/**
 * User Detail Modal — config + page binding
 *
 * Depends on: detail-modal.js (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 *             form-modal.js   (FormModal)
 */
(function () {
    'use strict';

    /* ── Detail modal config ──────────────────────────────── */
    var UserDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen kullanıcı kaydına ait detaylar',

        header: {
            icon: 'users',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; },
                    variant: function (r) { return r.isActive ? 'success' : 'danger'; }
                },
                {
                    label: function (r) { return 'Silinmiş: ' + (r.isDeleted ? 'Evet' : 'Hayır'); },
                    variant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; }
                }
            ]
        },

        sections: [
            {
                columns: 2,
                fields: [
                    {
                        label: 'Kullanıcı Adı',
                        icon: 'field-text',
                        value: function (r) { return r.username || '—'; }
                    },
                    {
                        label: 'Rol',
                        icon: 'shield',
                        value: function (r) { return r.roleName || '—'; }
                    },
                    {
                        label: 'Aktif',
                        icon: 'check-circle',
                        value: function (r) { return r.isActive ? 'Aktif' : 'Pasif'; },
                        badgeVariant: function (r) { return r.isActive ? 'success' : 'danger'; }
                    },
                    {
                        label: 'Silinmiş',
                        icon: 'shield',
                        value: function (r) { return r.isDeleted ? 'Evet' : 'Hayır'; },
                        badgeVariant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; }
                    },
                    {
                        label: 'Oluşturulma Tarihi',
                        icon: 'calendar',
                        value: function (r) { return DmFmt.date(r.createdAt); }
                    },
                    {
                        label: 'Oluşturan',
                        icon: 'user',
                        value: function (r) { return r.createdBy ? 'Admin (ID: ' + r.createdBy + ')' : '—'; }
                    },
                    {
                        label: 'Güncellenme Tarihi',
                        icon: 'calendar',
                        value: function (r) { return DmFmt.date(r.updatedAt); }
                    },
                    {
                        label: 'Güncelleyen',
                        icon: 'user',
                        value: function (r) { return r.updatedBy ? 'Admin (ID: ' + r.updatedBy + ')' : '—'; }
                    }
                ]
            },
            {
                title: 'Sistem Bilgileri',
                icon: 'database',
                columns: 2,
                fields: [
                    {
                        label: 'CreatedAt (UTC)',
                        icon: 'clock',
                        value: function (r) { return DmFmt.dateUtc(r.createdAt); }
                    },
                    {
                        label: 'UpdatedAt (UTC)',
                        icon: 'clock',
                        value: function (r) { return DmFmt.dateUtc(r.updatedAt); }
                    },
                    {
                        label: 'DeletedAt',
                        icon: 'clock',
                        value: function (r) { return r.deletedAt ? DmFmt.date(r.deletedAt) : '—'; }
                    }
                ]
            }
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            { key: 'edit',  label: 'Düzenle', icon: 'pencil', variant: 'primary', disabled: false, hidden: function(r) { return r.isDeleted || !r.isActive; } }
        ]
    };

    /* ── Role options helper ──────────────────────────────── */
    function getRoleOptions() {
        var roles = (window.__userMeta || {}).roles || [];
        return roles.map(function (r) {
            return { value: String(r.id), label: r.name || '' };
        });
    }

    /* ── Form field configs ───────────────────────────────── */
    function buildCreateFields() {
        return [
            {
                name: 'username',
                label: 'Kullanıcı Adı',
                type: 'text',
                required: true,
                maxLength: 100,
                placeholder: 'Örn: johndoe'
            },
            {
                name: 'password',
                label: 'Şifre',
                type: 'password',
                required: true,
                maxLength: 100,
                placeholder: 'Şifre giriniz'
            },
            {
                name: 'roleId',
                label: 'Rol',
                type: 'searchable-select',
                required: true,
                options: getRoleOptions()
            }
        ];
    }

    function buildEditFields() {
        return [
            {
                name: 'username',
                label: 'Kullanıcı Adı',
                type: 'text',
                required: true,
                maxLength: 100,
                placeholder: 'Örn: johndoe'
            },
            {
                name: 'password',
                label: 'Şifre',
                type: 'password',
                required: false,
                maxLength: 100,
                placeholder: 'Boş bırakılırsa değiştirilmez'
            },
            {
                name: 'roleId',
                label: 'Rol',
                type: 'searchable-select',
                required: true,
                options: getRoleOptions()
            }
        ];
    }

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Kullanıcı Ekle',
            description: 'Yeni bir kullanıcı kaydı oluşturun.',
            submitUrl: '/User/Create',
            submitLabel: 'Ekle',
            fields: buildCreateFields(),
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Kullanıcı Düzenle',
            description: 'Mevcut kullanıcı kaydını güncelleyin.',
            submitUrl: '/User/Update/' + id,
            submitLabel: 'Güncelle',
            fields: buildEditFields(),
            onSuccess: onSuccess
        };
    }

    /* ── Page binding ─────────────────────────────────────── */

    function readRows() {
        var el = document.getElementById('__user-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__userMeta || {};
        var params = new URLSearchParams({
            searchUsername: meta.searchUsername || '',
            roleFilter:     meta.roleFilter     || '',
            activeFilter:   meta.activeFilter   || '',
            deletedFilter:  meta.deletedFilter  || '',
            dateFrom:       meta.dateFrom        || '',
            dateTo:         meta.dateTo          || '',
            pageNumber:     meta.pageNumber      || 1,
            pageSize:       meta.pageSize        || 10
        });

        fetch('/User/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('user-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
        })
        .catch(function (err) {
            console.error('reloadTable hatası:', err);
        });
    }

    var ACTION_MESSAGES = {
        Delete:       { success: 'Silme işlemi başarılı.',              error: 'Silme işlemi başarısız oldu.' },
        Restore:      { success: 'Geri yükleme işlemi başarılı.',       error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null,                                   error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create:       { success: 'Kullanıcı başarıyla oluşturuldu.',    error: 'Kullanıcı oluşturulamadı.' },
        Update:       { success: 'Kullanıcı başarıyla güncellendi.',    error: 'Kullanıcı güncellenemedi.' }
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
                    successMsg = isActive ? 'Kullanıcı pasife alındı.' : 'Kullanıcı aktife alındı.';
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

    function bindAll() {
        var rows = readRows();

        /* Görüntüle — detail modal */
        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;

                DetailModal.open(UserDetailConfig, record, function (key) {
                    if (key === 'edit') {
                        DetailModal.close();
                        openEditModal(record);
                    }
                });
            });
        });

        /* Düzenle — form modal */
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

        /* Sil / Geri Yükle / Aktife Al / Pasife Al — confirm modal */
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
                    actionLabel   = 'Kaydı Sil';
                    actionVariant = 'danger';
                } else if (action.indexOf('Restore') !== -1) {
                    actionLabel   = 'Geri Yükle';
                    actionVariant = 'success';
                } else if (action.indexOf('ToggleActive') !== -1) {
                    var isActive  = record ? record.isActive : false;
                    actionLabel   = isActive ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isActive ? 'warning' : 'success';
                } else {
                    actionLabel   = 'İşlemi Gerçekleştir';
                    actionVariant = 'neutral';
                }

                var capturedIsActive = (action.indexOf('ToggleActive') !== -1) ? (record ? record.isActive : false) : null;

                var recordName = record ? (record.username || 'ID: ' + record.id) : '—';

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

    /* Yeni kayıt ekleme */
    function openCreateModal() {
        FormModal.open(buildCreateConfig(function () {
            if (typeof showToast === 'function') {
                showToast('success', 'Başarılı', ACTION_MESSAGES.Create.success);
            }
            reloadTable();
        }), {});
    }

    /* Kayıt güncelleme */
    function openEditModal(record) {
        FormModal.open(buildEditConfig(record.id, function () {
            if (typeof showToast === 'function') {
                showToast('success', 'Başarılı', ACTION_MESSAGES.Update.success);
            }
            reloadTable();
        }), {
            username: record.username || '',
            // password intentionally left blank — not pre-filled for security
            roleId: record.roleId
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll();

        /* Yeni Kayıt Ekle butonu */
        var addBtn = document.getElementById('user-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', openCreateModal);
        }
    });
})();
