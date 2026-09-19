(function () {
    'use strict';

    var TagDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen etiket kaydına ait detaylar',

        header: {
            icon: 'tags',
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
                        label: 'Ad',
                        icon: 'field-text',
                        value: function (r) { return r.name || '—'; }
                    },
                    {
                        label: 'Adres',
                        icon: 'field-text',
                        value: function (r) { return r.slug ? '/etiket/' + r.slug : '—'; }
                    },
                    {
                        label: 'Bağlı Yazı',
                        icon: 'field-number',
                        value: function (r) {
                            return r.postCount > 0
                                ? r.postCount + ' yazı'
                                : 'Hiçbir yayındaki yazıda kullanılmıyor';
                        },
                        badgeVariant: function (r) { return r.postCount > 0 ? 'primary' : 'warning'; }
                    }
                ]
            },
            DmAudit.section()
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            { key: 'edit', label: 'Düzenle', icon: 'pencil', variant: 'primary', hidden: function (r) { return r.isDeleted || !r.isActive; } }
        ]
    };

    var TAG_FORM_FIELDS = [
        {
            name: 'name',
            label: 'Ad',
            type: 'text',
            required: true,
            maxLength: 80,
            placeholder: 'Örn: EF Core',
            helpText: 'Etiket adı benzersizdir; aynı adla ikinci bir etiket açılamaz.'
        }
    ];

    var SLUG_FIELD = {
        name: 'slug',
        label: 'Adres',
        type: 'text',
        required: false,
        maxLength: 120,
        placeholder: 'Boş bırakın — mevcut adres korunur',
        helpText: 'Etiketin kalıcı adresi (/etiket/…). Ad değişince kendiliğinden değişmez. Buradan değiştirirseniz eski adres 404 verir.'
    };

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Etiket Ekle',
            description: 'Yeni bir etiket kaydı oluşturun.',
            submitUrl: '/Tag/Create',
            submitLabel: 'Ekle',
            fields: TAG_FORM_FIELDS,
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Etiket Düzenle',
            description: 'Mevcut etiket kaydını güncelleyin.',
            submitUrl: '/Tag/Update/' + id,
            submitLabel: 'Güncelle',
            fields: TAG_FORM_FIELDS.concat([SLUG_FIELD]),
            onSuccess: onSuccess
        };
    }

    function readRows() {
        var el = document.getElementById('__tag-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        FtList.reload();
    }

    var ACTION_MESSAGES = {
        Delete: { success: 'Silme işlemi başarılı.', error: 'Silme işlemi başarısız oldu.' },
        Restore: { success: 'Geri yükleme işlemi başarılı.', error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null, error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create: { success: 'Etiket oluşturuldu.', error: 'Etiket oluşturulamadı.' },
        Update: { success: 'Etiket güncellendi.', error: 'Etiket güncellenemedi.' }
    };

    function resolveActionKey(actionUrl) {
        if (actionUrl.indexOf('Delete') !== -1) return 'Delete';
        if (actionUrl.indexOf('Restore') !== -1) return 'Restore';
        if (actionUrl.indexOf('ToggleActive') !== -1) return 'ToggleActive';
        return null;
    }

    function toast(kind, title, message) {
        if (typeof showToast === 'function') showToast(kind, title, message);
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
                        successMsg = isActive ? 'Etiket pasife alındı.' : 'Etiket aktife alındı.';
                    }
                    toast('success', 'Başarılı', successMsg || 'İşlem başarıyla tamamlandı.');
                    reloadTable();
                } else {
                    return r.text().then(function (body) {
                        var serverMsg = '';
                        try { serverMsg = JSON.parse(body).message || ''; } catch (e) { serverMsg = ''; }
                        toast('error', 'Hata', serverMsg || msgs.error || 'İşlem başarısız oldu.');
                    });
                }
            })
            .catch(function () {
                toast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.');
            });
    }

    function openCreateModal() {
        FormModal.open(buildCreateConfig(function () {
            toast('success', 'Başarılı', ACTION_MESSAGES.Create.success);
            reloadTable();
        }), {});
    }

    function openEditModal(record) {
        FormModal.open(buildEditConfig(record.id, function () {
            toast('success', 'Başarılı', ACTION_MESSAGES.Update.success);
            reloadTable();
        }), {
            name: record.name || '',
            slug: record.slug || ''
        });
    }

    function bindAll() {
        var rows = readRows();

        function findRow(id) {
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].id === id) return rows[i];
            }
            return null;
        }

        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var record = findRow(parseInt(btn.dataset.id, 10));
                if (!record) return;

                DetailModal.open(TagDetailConfig, record, function (key) {
                    if (key === 'edit') {
                        DetailModal.close();
                        openEditModal(record);
                    }
                });
            });
        });

        document.querySelectorAll('.ft-edit-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var record = findRow(parseInt(btn.dataset.id, 10));
                if (record) openEditModal(record);
            });
        });

        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();

                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10);
                var record = findRow(id);
                var action = form.action || '';
                var actionLabel, actionVariant, note;

                if (action.indexOf('Delete') !== -1) {
                    actionLabel = 'Kaydı Sil';
                    actionVariant = 'danger';
                    if (record && record.postCount > 0) {
                        note = 'Bu etiket ' + record.postCount + ' yazıda kullanılıyor; silinince o yazılarda görünmez olur.';
                    }
                } else if (action.indexOf('Restore') !== -1) {
                    actionLabel = 'Geri Yükle';
                    actionVariant = 'success';
                } else if (action.indexOf('ToggleActive') !== -1) {
                    var isActive = record ? record.isActive : false;
                    actionLabel = isActive ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isActive ? 'warning' : 'success';
                    if (isActive && record && record.postCount > 0) {
                        note = 'Pasife alınan etiket ' + record.postCount + ' yazıda ve etiket sayfasında görünmez olur; bağlar durur.';
                    }
                } else {
                    actionLabel = 'İşlemi Gerçekleştir';
                    actionVariant = 'neutral';
                }

                var capturedIsActive = (action.indexOf('ToggleActive') !== -1)
                    ? (record ? record.isActive : false)
                    : null;

                ConfirmModal.open({
                    id: id,
                    recordLabel: 'Etiket:',
                    email: record ? (record.name || 'ID: ' + id) : '—',
                    actionLabel: actionLabel,
                    actionVariant: actionVariant,
                    note: note,
                    onConfirm: function () { submitAction(form, capturedIsActive); }
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('ft:table-rendered', bindAll);
        bindAll();

        var addBtn = document.getElementById('tag-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', openCreateModal);
        }
    });

})();
