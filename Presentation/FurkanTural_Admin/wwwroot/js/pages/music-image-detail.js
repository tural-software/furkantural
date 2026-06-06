/**
 * MusicImage Detail Modal — config + page binding
 *
 * Depends on: detail-modal.js (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 *             form-modal.js   (FormModal — file/hidden/checkbox support)
 */
(function () {
    'use strict';

    /* ── Detail modal config ──────────────────────────────── */
    var MusicImageDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen müzik görseli kaydına ait detaylar',

        header: {
            icon: 'music-images',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; },
                    variant: function (r) { return r.isActive ? 'success' : 'danger'; }
                },
                {
                    label: function (r) { return r.isCover ? '★ Kapak' : 'Kapak Değil'; },
                    variant: function (r) { return r.isCover ? 'accent' : 'neutral'; }
                }
            ]
        },

        sections: [
            {
                columns: 1,
                fields: [
                    {
                        label: 'Görsel URL',
                        icon: 'field-text',
                        value: function (r) { return r.url || '—'; },
                        multiline: true
                    },
                    {
                        label: 'Açıklama (AltText)',
                        icon: 'field-text',
                        value: function (r) { return r.altText || '—'; }
                    }
                ]
            },
            {
                columns: 2,
                fields: [
                    {
                        label: 'Müzik ID',
                        icon: 'hash-icon',
                        value: function (r) { return String(r.musicId); }
                    },
                    {
                        label: 'Kapak Görseli',
                        icon: 'shield',
                        value: function (r) { return r.isCover ? 'Evet' : 'Hayır'; },
                        badgeVariant: function (r) { return r.isCover ? 'accent' : 'neutral'; }
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
            { key: 'close', label: 'Kapat',    variant: 'secondary' },
            { key: 'edit',  label: 'Düzenle',  icon: 'pencil', variant: 'primary', hidden: function(r) { return r.isDeleted || !r.isActive; } }
        ],

        imagePreview: function (r) {
            if (!r.url) return '';
            var base = (window.__apiBaseUrl || '').replace(/\/$/, '');
            var rel  = r.url.replace(/^\//, '');
            var src  = r.url.startsWith('http')
                ? r.url
                : (rel.indexOf('/') >= 0 ? base + '/' + rel : base + '/images/uploads/' + rel);
            var safe = src.replace(/&/g, '&amp;').replace(/"/g, '&quot;');
            var alt  = (r.altText || 'Görsel').replace(/&/g, '&amp;').replace(/"/g, '&quot;');
            return '<div class="dm-image-preview"><img src="' + safe + '" alt="' + alt + '" /></div>';
        }
    };

    /* ── Form field configs ───────────────────────────────── */
    var _musicOptions = null;

    function fetchMusicOptions(callback) {
        if (_musicOptions !== null) { callback(_musicOptions); return; }
        fetch('/MusicImage/MusicOptions', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) { _musicOptions = data || []; callback(_musicOptions); })
            .catch(function () { _musicOptions = []; callback([]); });
    }

    function buildMusicIdField(options) {
        return {
            name: 'musicId',
            label: 'Müzik',
            type: 'searchable-select',
            required: true,
            placeholder: 'Müzik seçin veya arayın...',
            options: options
        };
    }

    function buildCreateFields(options) {
        return [
            {
                name: 'imageFile',
                label: 'Görsel Dosyası',
                type: 'file',
                required: true,
                accept: 'image/*',
                maxSizeBytes: 5 * 1024 * 1024,
                helpText: 'PNG, JPG, JPEG, WebP formatları desteklenir. Maks. 5 MB.'
            },
            {
                name: 'altText',
                label: 'Açıklama Metni (AltText)',
                type: 'text',
                required: false,
                maxLength: 500,
                placeholder: 'Görsel açıklaması...'
            },
            {
                name: 'isCover',
                label: 'Kapak Görseli',
                type: 'checkbox',
                required: false
            },
            buildMusicIdField(options)
        ];
    }

    function buildEditFields(options) {
        return [
            {
                name: 'imageFile',
                label: 'Yeni Görsel Dosyası',
                type: 'file',
                required: false,
                accept: 'image/*',
                maxSizeBytes: 5 * 1024 * 1024,
                helpText: 'Boş bırakırsanız mevcut görsel korunur. Maks. 5 MB.'
            },
            {
                name: 'altText',
                label: 'Açıklama Metni (AltText)',
                type: 'text',
                required: false,
                maxLength: 500,
                placeholder: 'Görsel açıklaması...'
            },
            {
                name: 'isCover',
                label: 'Kapak Görseli',
                type: 'checkbox',
                required: false
            },
            buildMusicIdField(options)
        ];
    }

    function buildCreateConfig(options, onSuccess) {
        return {
            title: 'Yeni Müzik Görseli Ekle',
            description: 'Yeni bir müzik görseli kaydı oluşturun.',
            submitUrl: '/MusicImage/Create',
            submitLabel: 'Ekle',
            fields: buildCreateFields(options),
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, options, onSuccess) {
        return {
            title: 'Müzik Görselini Düzenle',
            description: 'Mevcut müzik görseli kaydını güncelleyin.',
            submitUrl: '/MusicImage/Update/' + id,
            submitLabel: 'Güncelle',
            fields: buildEditFields(options),
            onSuccess: onSuccess
        };
    }

    /* ── Page binding ─────────────────────────────────────── */

    function readRows() {
        var el = document.getElementById('__music-image-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__musicImageMeta || {};
        var params = new URLSearchParams({
            url:           meta.url           || '',
            isCoverFilter: meta.isCoverFilter || '',
            activeFilter:  meta.activeFilter  || '',
            deletedFilter: meta.deletedFilter || '',
            musicId:       meta.musicId       || '',
            dateFrom:      meta.dateFrom      || '',
            dateTo:        meta.dateTo        || '',
            pageNumber:    meta.pageNumber    || 1,
            pageSize:      meta.pageSize      || 10
        });

        fetch('/MusicImage/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('music-image-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
        })
        .catch(function (err) { console.error('reloadTable hatası:', err); });
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
                    if (typeof showToast === 'function') showToast('error', 'Hata', errorMsg);
                });
            }
        })
        .catch(function () {
            if (typeof showToast === 'function') showToast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.');
        });
    }

    function openEditModal(record) {
        fetchMusicOptions(function (options) {
            FormModal.open(
                buildEditConfig(record.id, options, function () {
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Update.success);
                    reloadTable();
                }),
                {
                    altText:  record.altText || '',
                    isCover:  record.isCover,
                    musicId:  record.musicId
                }
            );
        });
    }

    function openCreateModal() {
        fetchMusicOptions(function (options) {
            FormModal.open(
                buildCreateConfig(options, function () {
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', ACTION_MESSAGES.Create.success);
                    reloadTable();
                }),
                {}
            );
        });
    }

    function bindAll() {
        var rows = readRows();

        /* Görüntüle */
        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;

                DetailModal.open(MusicImageDetailConfig, record, function (key) {
                    if (key === 'edit') {
                        DetailModal.close();
                        openEditModal(record);
                    }
                });
            });
        });

        /* Düzenle */
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

        /* Row action forms — confirm modal */
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
                    var isActive  = record ? record.isActive : false;
                    actionLabel   = isActive ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isActive ? 'warning' : 'success';
                } else {
                    actionLabel = 'İşlemi Gerçekleştir'; actionVariant = 'neutral';
                }

                var capturedIsActive = (action.indexOf('ToggleActive') !== -1)
                    ? (record ? record.isActive : false)
                    : null;

                var recordName = record ? ('ID: ' + record.id + (record.url ? ' | ' + record.url : '')) : '—';

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

        var addBtn = document.getElementById('music-image-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', openCreateModal);
        }
    });
})();
