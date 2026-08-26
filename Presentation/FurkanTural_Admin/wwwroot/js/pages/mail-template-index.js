/**
 * MailTemplate Index — modal binding
 *
 * Depends on: detail-modal.js  (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 *             form-modal.js    (FormModal)
 */
(function () {
    'use strict';

    var MailTemplateDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen şablon kaydına ait detaylar',

        header: {
            icon: 'mail-template',
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
                        label: 'Posta Türü',
                        icon: 'mail-template',
                        value: function (r) { return r.typeName || r.typeCode || '—'; },
                        badgeVariant: function (r) {
                            return (r.placeholders && r.placeholders.length) ? 'primary' : 'warning';
                        }
                    },
                    {
                        label: 'Proje',
                        icon: 'mail-template',
                        value: function (r) { return r.appSourceName || 'Tüm projeler (genel)'; },
                        badgeVariant: function (r) { return r.appSourceId ? 'primary' : 'neutral'; }
                    },
                    {
                        label: 'Konu',
                        icon: 'field-text',
                        value: function (r) { return r.subject || '—'; }
                    },
                    {
                        label: 'Kullanılabilir Alanlar',
                        icon: 'field-text',
                        value: function (r) {
                            if (!r.placeholders || !r.placeholders.length) return 'yok — bu türü gönderen kod yolu bulunmuyor';
                            return r.placeholders.map(function (p) { return '{{' + p + '}}'; }).join(' ');
                        }
                    },
                    {
                        label: 'Dosya Adı',
                        icon: 'file-text',
                        value: function (r) { return r.fileName || '—'; }
                    }
                ]
            },
            DmAudit.section()
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            {
                key: 'preview',
                label: 'Taslağı Görüntüle',
                icon: 'file-text',
                hidden: function (r) { return r.isDeleted; }
            },
            {
                key: 'edit',
                label: 'Düzenle',
                icon: 'pencil',
                variant: 'primary',
                hidden: function (r) { return r.isDeleted || !r.isActive; }
            }
        ]
    };

    function allTypes() {
        var meta = window.__mailTemplateMeta || {};
        return meta.types || [];
    }

    function typeOptions() {
        return allTypes().map(function (t) {
            return { value: String(t.id), label: t.placeholders && t.placeholders.length ? t.name : t.name + ' (gönderilmiyor)' };
        });
    }

    function allAppSources() {
        var meta = window.__mailTemplateMeta || {};
        return meta.appSources || [];
    }

    function appSourceOptions() {
        var options = [{ value: '', label: 'Tüm projeler (genel)' }];
        allAppSources().forEach(function (a) {
            options.push({ value: String(a.id), label: a.name || a.code });
        });
        return options;
    }

    function placeholderHint(base) {
        var names = allTypes().reduce(function (acc, t) {
            (t.placeholders || []).forEach(function (p) { if (acc.indexOf(p) === -1) acc.push(p); });
            return acc;
        }, []);
        if (!names.length) return base + '...';
        return base + ' — kullanılabilir: ' + names.map(function (n) { return '{{' + n + '}}'; }).join(' ');
    }

    var TEMPLATE_FORM_FIELDS = [
        {
            name: 'name',
            label: 'Ad',
            type: 'text',
            required: false,
            maxLength: 200,
            placeholder: 'Şablon adı'
        },
        {
            name: 'mailTemplateTypeId',
            label: 'Posta Türü',
            type: 'searchable-select',
            required: true,
            placeholder: 'Tür seçin...',
            options: typeOptions()
        },
        {
            name: 'appSourceId',
            label: 'Proje',
            type: 'searchable-select',
            required: false,
            placeholder: 'Tüm projeler (genel)',
            options: appSourceOptions()
        },
        {
            name: 'subject',
            label: 'Konu',
            type: 'text',
            required: true,
            maxLength: 300,
            placeholder: placeholderHint('Konu satırı; yer tutucu kullanılabilir')
        },
        {
            name: 'fileName',
            label: 'Dosya Adı',
            type: 'text',
            required: false,
            maxLength: 200,
            placeholder: 'örn: owner-template.html'
        },
        {
            name: 'htmlContent',
            label: 'HTML İçeriği',
            type: 'textarea',
            required: true,
            rows: 16,
            placeholder: placeholderHint('E-posta HTML içeriğini buraya girin')
        }
    ];

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Şablon Ekle',
            description: 'Yeni bir e-posta şablonu oluşturun.',
            submitUrl: '/MailTemplate/Create',
            submitLabel: 'Ekle',
            size: 'large',
            fields: TEMPLATE_FORM_FIELDS,
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Şablon Düzenle',
            description: 'Mevcut e-posta şablonunu güncelleyin.',
            submitUrl: '/MailTemplate/Update/' + id,
            submitLabel: 'Güncelle',
            size: 'large',
            fields: TEMPLATE_FORM_FIELDS,
            onSuccess: onSuccess
        };
    }

    function readRows() {
        var el = document.getElementById('__mail-template-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__mailTemplateMeta || {};
        var params = new URLSearchParams({
            name:          meta.name          || '',
            activeFilter:  meta.activeFilter  || '',
            deletedFilter: meta.deletedFilter || '',
            dateFrom:      meta.dateFrom      || '',
            dateTo:        meta.dateTo        || '',
            pageNumber:    meta.pageNumber    || 1,
            pageSize:      meta.pageSize      || 10
        });

        fetch('/MailTemplate/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('mail-template-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
        })
        .catch(function (err) {
            console.error('reloadTable hatası:', err);
        });
    }

    var ACTION_MESSAGES = {
        Delete:       { success: 'Silme işlemi başarılı.',         error: 'Silme işlemi başarısız oldu.' },
        Restore:      { success: 'Geri yükleme işlemi başarılı.',  error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null,                              error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create:       { success: 'Kayıt başarıyla oluşturuldu.',   error: 'Kayıt oluşturulamadı.' },
        Update:       { success: 'Kayıt başarıyla güncellendi.',   error: 'Kayıt güncellenemedi.' }
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
                    successMsg = isActive ? 'Şablon pasife alındı.' : 'Şablon aktife alındı.';
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

        function findRow(id) {
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].id === id) return rows[i];
            }
            return null;
        }

        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = findRow(id);
                if (!record) return;

                DetailModal.open(MailTemplateDetailConfig, record, function (key) {
                    if (key === 'edit') {
                        DetailModal.close();
                        openEditModal(record);
                    }
                    if (key === 'preview') {
                        openPreview(record);
                    }
                });
            });
        });

        document.querySelectorAll('.ft-edit-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = findRow(id);
                if (!record) return;
                openEditModal(record);
            });
        });

        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();

                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10);
                var record = findRow(id);
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

                var capturedIsActive = (action.indexOf('ToggleActive') !== -1)
                    ? (record ? record.isActive : false)
                    : null;

                var recordName = record ? (record.name || 'ID: ' + (record.id || id)) : '—';

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

    function openPreview(record) {
        fetch('/MailTemplate/PreviewHtml/' + record.id, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
            if (!r.ok) throw new Error('PreviewHtml ' + r.status);
            return r.json();
        })
        .then(function (data) {
            if (!data) return;
            DetailModal.close();
            HtmlPreviewModal.open(record.name || 'Şablon Önizlemesi', data.htmlContent || '');
        })
        .catch(function () {
            if (typeof showToast === 'function') {
                showToast('error', 'Hata', 'Şablon içeriği yüklenemedi.');
            }
        });
    }

    function openCreateModal() {
        FormModal.open(buildCreateConfig(function () {
            if (typeof showToast === 'function') {
                showToast('success', 'Başarılı', ACTION_MESSAGES.Create.success);
            }
            reloadTable();
        }), {});
    }

    function openEditModal(record) {
        FormModal.open(buildEditConfig(record.id, function () {
            if (typeof showToast === 'function') {
                showToast('success', 'Başarılı', ACTION_MESSAGES.Update.success);
            }
            reloadTable();
        }), {
            name:               record.name               || '',
            mailTemplateTypeId: String(record.mailTemplateTypeId || ''),
            appSourceId:        record.appSourceId ? String(record.appSourceId) : '',
            subject:            record.subject            || '',
            fileName:           record.fileName           || '',
            htmlContent:        record.htmlContent        || ''
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll();

        var addBtn = document.getElementById('mail-template-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', openCreateModal);
        }
    });

})();
