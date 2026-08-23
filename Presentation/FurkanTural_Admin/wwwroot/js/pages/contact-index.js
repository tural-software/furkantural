/**
 * Contact Index — modal & action binding
 *
 * Depends on: detail-modal.js  (DetailModal, DmFmt)
 *             confirm-modal.js (ConfirmModal)
 */
(function () {
    'use strict';

    var ContactDetailConfig = {
        title: 'Mesaj Detayı',
        description: 'Gönderilen iletişim mesajına ait detaylar',

        header: {
            icon: 'contact',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return r.isRead ? '✓ Okundu' : '✉ Okunmadı'; },
                    variant: function (r) { return r.isRead ? 'success' : 'warning'; }
                },
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
                        icon: 'user',
                        value: function (r) { return r.name || '—'; }
                    },
                    {
                        label: 'E-Posta',
                        icon: 'mail',
                        value: function (r) { return r.email || '—'; }
                    },
                    {
                        label: 'Okundu',
                        icon: 'check-circle',
                        value: function (r) { return r.isRead ? 'Okundu' : 'Okunmadı'; },
                        badgeVariant: function (r) { return r.isRead ? 'success' : 'warning'; }
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
                    }
                ]
            },
            {
                title: 'Mesaj İçeriği',
                icon: 'contact',
                columns: 1,
                fields: [
                    {
                        label: 'Mesaj',
                        icon: 'field-text',
                        value: function (r) { return r.message || '—'; },
                        isCode: true
                    }
                ]
            },
            {
                title: 'Teknik Bilgiler',
                icon: 'database',
                columns: 2,
                fields: [
                    {
                        label: 'IP Adresi',
                        icon: 'link',
                        value: function (r) { return r.ipAddress || '—'; }
                    },
                    {
                        label: 'User Agent',
                        icon: 'logs',
                        value: function (r) { return r.userAgent || '—'; },
                        isCode: true
                    },
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
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__contact-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__contactMeta || {};
        var params = new URLSearchParams({
            name:          meta.name          || '',
            activeFilter:  meta.activeFilter  || '',
            deletedFilter: meta.deletedFilter || '',
            readFilter:    meta.readFilter    || '',
            dateFrom:      meta.dateFrom      || '',
            dateTo:        meta.dateTo        || '',
            pageNumber:    meta.pageNumber    || 1,
            pageSize:      meta.pageSize      || 10
        });

        fetch('/Contact/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('contact-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
        })
        .catch(function (err) {
            console.error('reloadTable hatası:', err);
        });
    }

    function getToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function sendAction(url, id, onSuccess, onError) {
        var data = new FormData();
        data.append('id', id);
        data.append('__RequestVerificationToken', getToken());

        fetch(url, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: data
        })
        .then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
            if (r.ok) {
                onSuccess();
                reloadTable();
            } else {
                return r.text().then(function (body) {
                    var serverMsg = '';
                    try { serverMsg = JSON.parse(body).message || ''; } catch (e) { serverMsg = ''; }
                    onError(serverMsg);
                });
            }
        })
        .catch(function () {
            onError('Sunucudan beklenmeyen bir hata döndü.');
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

        function recordLabel(record, id) {
            return record ? (record.name || record.email || 'ID: ' + id) : '—';
        }

        document.querySelectorAll('.row-btn[data-contact-id][title="Detay"]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.contactId, 10);
                var record = findRow(id);
                if (!record) return;
                DetailModal.open(ContactDetailConfig, record);
            });
        });

        /* Okundu İşaretle — direct, no confirm */
        document.querySelectorAll('.row-btn--read[data-contact-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.contactId, 10);
                sendAction(
                    '/Contact/MarkAsRead', id,
                    function () {
                        if (typeof showToast === 'function') {
                            showToast('success', 'Başarılı', 'Mesaj okundu olarak işaretlendi.');
                        }
                    },
                    function (msg) {
                        if (typeof showToast === 'function') {
                            showToast('error', 'Hata', msg || 'Okundu işareti başarısız oldu.');
                        }
                    }
                );
            });
        });

        document.querySelectorAll('.row-btn--warn[data-contact-id], .row-btn--success[data-contact-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.contactId, 10);
                var record = findRow(id);
                var isActive = record ? record.isActive : btn.classList.contains('row-btn--warn');
                ConfirmModal.open({
                    id: id,
                    email: recordLabel(record, id),
                    actionLabel: isActive ? 'Pasife Al' : 'Aktife Al',
                    actionVariant: isActive ? 'warning' : 'success',
                    onConfirm: function () {
                        sendAction(
                            '/Contact/ToggleActive', id,
                            function () {
                                var msg = isActive ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.';
                                if (typeof showToast === 'function') showToast('success', 'Başarılı', msg);
                            },
                            function (msg) {
                                if (typeof showToast === 'function') {
                                    showToast('error', 'Hata', msg || 'Durum değiştirme işlemi başarısız oldu.');
                                }
                            }
                        );
                    }
                });
            });
        });

        document.querySelectorAll('.row-btn--delete[data-contact-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.contactId, 10);
                var record = findRow(id);
                ConfirmModal.open({
                    id: id,
                    email: recordLabel(record, id),
                    actionLabel: 'Kaydı Sil',
                    actionVariant: 'danger',
                    onConfirm: function () {
                        sendAction(
                            '/Contact/Delete', id,
                            function () {
                                if (typeof showToast === 'function') showToast('success', 'Başarılı', 'Silme işlemi başarılı.');
                            },
                            function (msg) {
                                if (typeof showToast === 'function') {
                                    showToast('error', 'Hata', msg || 'Silme işlemi başarısız oldu.');
                                }
                            }
                        );
                    }
                });
            });
        });

        document.querySelectorAll('.row-btn--restore[data-contact-id]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.contactId, 10);
                var record = findRow(id);
                ConfirmModal.open({
                    id: id,
                    email: recordLabel(record, id),
                    actionLabel: 'Geri Yükle',
                    actionVariant: 'success',
                    onConfirm: function () {
                        sendAction(
                            '/Contact/Restore', id,
                            function () {
                                if (typeof showToast === 'function') showToast('success', 'Başarılı', 'Geri yükleme işlemi başarılı.');
                            },
                            function (msg) {
                                if (typeof showToast === 'function') {
                                    showToast('error', 'Hata', msg || 'Geri yükleme işlemi başarısız oldu.');
                                }
                            }
                        );
                    }
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll();
    });

})();
