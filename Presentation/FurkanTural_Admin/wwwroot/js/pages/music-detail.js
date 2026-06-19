/**
 * Music Detail Modal — config + page binding
 *
 * Depends on: detail-modal.js (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 *             form-modal.js   (FormModal)
 */
(function () {
    'use strict';

    /* ── Date helper ──────────────────────────────────────── */
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

    /* ── Detail modal config ──────────────────────────────── */
    var MusicDetailConfig = {
        title: 'Kayıt Detayı',
        description: 'Seçilen müzik kaydına ait detaylar',

        header: {
            icon: 'music',
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
                        label: 'Şarkı Adı',
                        icon: 'field-text',
                        value: function (r) { return r.name || '—'; }
                    },
                    {
                        label: 'Sanatçı',
                        icon: 'field-text',
                        value: function (r) { return r.artist || '—'; }
                    },
                    {
                        label: 'Prodüktör',
                        icon: 'field-text',
                        value: function (r) { return r.productor || '—'; }
                    },
                    {
                        label: 'Albüm',
                        icon: 'field-text',
                        value: function (r) { return r.album || '—'; }
                    },
                    {
                        label: 'Tür',
                        icon: 'field-text',
                        value: function (r) { return r.genre || '—'; }
                    },
                    {
                        label: 'Süre',
                        icon: 'clock',
                        value: function (r) { return r.duration || '—'; }
                    },
                    {
                        label: 'Yayın Tarihi',
                        icon: 'calendar',
                        value: function (r) { return r.releaseDate ? DmFmt.date(r.releaseDate) : '—'; }
                    },
                    {
                        label: 'YouTube Music',
                        icon: 'link',
                        value: function (r) { return r.youTubeMusicUrl || '—'; }
                    }
                ]
            },
            {
                columns: 1,
                fields: [
                    {
                        label: 'Sözler',
                        icon: 'field-text',
                        value: function (r) { return r.lyrics || '—'; },
                        multiline: true
                    }
                ]
            },
            {
                columns: 2,
                fields: [
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

    /* ── Form field configs ───────────────────────────────── */
    var MUSIC_FORM_FIELDS_BASE = [
        { name: 'name',       label: 'Şarkı Adı',  type: 'text',     required: false, maxLength: 200, placeholder: 'Şarkı adını girin...' },
        { name: 'artist',     label: 'Sanatçı',    type: 'text',     required: false, maxLength: 200, placeholder: 'Sanatçı adını girin...' },
        { name: 'productor',  label: 'Prodüktör',  type: 'text',     required: false, maxLength: 200, placeholder: 'Prodüktör adını girin...' },
        { name: 'album',      label: 'Albüm',      type: 'text',     required: false, maxLength: 200, placeholder: 'Albüm adını girin...' },
        { name: 'genre',      label: 'Tür',        type: 'text',     required: false, maxLength: 200, placeholder: 'Müzik türünü girin...' },
        { name: 'lyrics',     label: 'Sözler',     type: 'textarea', required: false, rows: 12, placeholder: 'Şarkı sözlerini girin...' },
        { name: 'duration',   label: 'Süre',       type: 'text',     required: false, maxLength: 12,  placeholder: 'SS:DD:SN (ör: 00:04:10)' },
        { name: 'releaseDate', label: 'Yayın Tarihi', type: 'date',  required: false },
        { name: 'youTubeMusicUrl', label: 'YouTube Music URL', type: 'text', required: false, maxLength: 500, placeholder: 'https://music.youtube.com/watch?v=...' }
    ];

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Müzik Ekle',
            description: 'Yeni bir müzik kaydı oluşturun.',
            submitUrl: '/Music/Create',
            submitLabel: 'Ekle',
            size: 'large',
            fields: MUSIC_FORM_FIELDS_BASE,
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Müziği Düzenle',
            description: 'Mevcut müzik kaydını güncelleyin.',
            submitUrl: '/Music/Update/' + id,
            submitLabel: 'Güncelle',
            size: 'large',
            fields: MUSIC_FORM_FIELDS_BASE,
            onSuccess: onSuccess
        };
    }

    /* ── Page binding ─────────────────────────────────────── */

    function readRows() {
        var el = document.getElementById('__music-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__musicMeta || {};
        var params = new URLSearchParams({
            searchName:   meta.searchName   || '',
            searchArtist: meta.searchArtist || '',
            activeFilter: meta.activeFilter || '',
            deletedFilter:meta.deletedFilter|| '',
            musicId:      meta.musicId      || '',
            dateFrom:     meta.dateFrom     || '',
            dateTo:       meta.dateTo       || '',
            pageNumber:   meta.pageNumber   || 1,
            pageSize:     meta.pageSize     || 10
        });

        fetch('/Music/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('music-table-section');
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
                name:        record.name        || '',
                artist:      record.artist      || '',
                productor:   record.productor   || '',
                album:       record.album       || '',
                genre:       record.genre       || '',
                lyrics:      record.lyrics      || '',
                duration:    record.duration    || '',
                releaseDate: toDateInput(record.releaseDate),
                youTubeMusicUrl: record.youTubeMusicUrl || ''
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

        /* Görüntüle — detail modal */
        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;
                DetailModal.open(MusicDetailConfig, record, function (key) {
                    if (key === 'edit') { DetailModal.close(); openEditModal(record); }
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

        /* Delete / ToggleActive / Restore — confirm modal */
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
                var recordName = record ? (record.name || 'ID: ' + record.id) : '—';
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
        var addBtn = document.getElementById('music-add-btn');
        if (addBtn) addBtn.addEventListener('click', openCreateModal);
    });
})();
