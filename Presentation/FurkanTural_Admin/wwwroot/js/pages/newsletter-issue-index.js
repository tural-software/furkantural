(function () {
    'use strict';

    var POLL_MS = 5000;
    var _pollTimer = null;

    function meta() { return window.__newsletterMeta || {}; }

    function audience() { return meta().audience || 0; }

    function token() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function stateLabel(status) {
        if (status === 'Draft') return 'Taslak';
        if (status === 'Sending') return 'Dağıtılıyor';
        if (status === 'Sent') return 'Gönderildi';
        return status || '—';
    }

    var NewsletterDetailConfig = {
        title: 'Bülten Detayı',
        description: 'Seçilen bülten sayısına ait detaylar',

        header: {
            icon: 'newsletters',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return stateLabel(r.status); },
                    variant: function (r) {
                        if (r.status === 'Sent') return 'success';
                        if (r.status === 'Sending') return 'primary';
                        return 'neutral';
                    }
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
                        label: 'Konu',
                        icon: 'field-text',
                        value: function (r) { return r.subject || '—'; }
                    },
                    {
                        label: 'Durum',
                        icon: 'circle-outline',
                        value: function (r) { return stateLabel(r.status); }
                    },
                    {
                        label: 'Dağıtıma Verildi',
                        icon: 'clock',
                        value: function (r) { return r.queuedAt ? DmFmt.date(r.queuedAt) : 'Henüz verilmedi'; }
                    },
                    {
                        label: 'Tamamlandı',
                        icon: 'clock',
                        value: function (r) { return r.completedAt ? DmFmt.date(r.completedAt) : '—'; }
                    },
                    {
                        label: 'Alıcı Sayısı',
                        icon: 'field-number',
                        value: function (r) { return r.status === 'Draft' ? 'Liste henüz dondurulmadı' : String(r.recipientCount); }
                    },
                    {
                        label: 'Sonuç',
                        icon: 'field-number',
                        value: function (r) {
                            if (r.status === 'Draft') return '—';
                            return r.sentCount + ' gönderildi · ' + r.failedCount + ' başarısız · ' + r.skippedCount + ' atlandı';
                        },
                        badgeVariant: function (r) { return r.failedCount > 0 ? 'warning' : 'neutral'; }
                    }
                ]
            },
            DmAudit.section()
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            {
                key: 'preview',
                label: 'Gövdeyi Görüntüle',
                icon: 'file-text'
            },
            {
                key: 'test',
                label: 'Deneme Gönder',
                icon: 'mail',
                hidden: function (r) { return r.isDeleted; }
            },
            {
                key: 'edit',
                label: 'Düzenle',
                icon: 'pencil',
                variant: 'primary',
                hidden: function (r) { return r.isDeleted || !r.isActive || r.status !== 'Draft'; }
            }
        ]
    };

    var ISSUE_FORM_FIELDS = [
        {
            name: 'subject',
            label: 'Konu',
            type: 'text',
            required: true,
            maxLength: 300,
            placeholder: 'Alıcının gelen kutusunda göreceği başlık'
        },
        {
            name: 'body',
            label: 'Gövde (HTML)',
            type: 'textarea',
            required: true,
            rows: 18,
            placeholder: 'Yazının kendisi. Başlık, imza ve zorunlu çıkış bağlantısı posta şablonundan gelir; buraya yalnızca içerik yazılır.'
        }
    ];

    function buildCreateConfig(onSuccess) {
        return {
            title: 'Yeni Bülten Yaz',
            description: 'Taslak olarak kaydedilir; dağıtıma ayrı bir adımda verilir.',
            submitUrl: '/NewsletterIssue/Create',
            submitLabel: 'Kaydet',
            size: 'large',
            fields: ISSUE_FORM_FIELDS,
            onSuccess: onSuccess
        };
    }

    function buildEditConfig(id, onSuccess) {
        return {
            title: 'Bülten Düzenle',
            description: 'Yalnızca taslak durumundaki bülten düzenlenebilir.',
            submitUrl: '/NewsletterIssue/Update/' + id,
            submitLabel: 'Güncelle',
            size: 'large',
            fields: ISSUE_FORM_FIELDS,
            onSuccess: onSuccess
        };
    }

    function buildTestConfig(id, onSuccess) {
        return {
            title: 'Deneme Gönder',
            description: 'Bülten tek bir adrese gönderilir. Dağıtım kaydı açılmaz, sayaçlar değişmez.',
            submitUrl: '/NewsletterIssue/SendTest/' + id,
            submitLabel: 'Gönder',
            fields: [
                {
                    name: 'email',
                    label: 'Adres',
                    type: 'text',
                    required: true,
                    maxLength: 256,
                    placeholder: 'ornek@furkantural.com'
                }
            ],
            onSuccess: onSuccess
        };
    }

    function readRows() {
        var el = document.getElementById('__newsletter-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        FtList.reload();
    }

    var ACTION_MESSAGES = {
        Delete:       { success: 'Silme işlemi başarılı.',        error: 'Silme işlemi başarısız oldu.' },
        Restore:      { success: 'Geri yükleme işlemi başarılı.', error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null,                            error: 'Durum değiştirme işlemi başarısız oldu.' },
        Create:       { success: 'Bülten taslağı oluşturuldu.',   error: 'Bülten oluşturulamadı.' },
        Update:       { success: 'Bülten güncellendi.',           error: 'Bülten güncellenemedi.' }
    };

    function resolveActionKey(actionUrl) {
        if (actionUrl.indexOf('Delete')       !== -1) return 'Delete';
        if (actionUrl.indexOf('Restore')      !== -1) return 'Restore';
        if (actionUrl.indexOf('ToggleActive') !== -1) return 'ToggleActive';
        return null;
    }

    function toast(kind, title, message) {
        if (typeof showToast === 'function') showToast(kind, title, message);
    }

    function submitAction(form, record) {
        var data = new FormData(form);
        var actionKey = resolveActionKey(form.action || '');
        var wasActive = record ? record.isActive : false;
        var wasSending = record ? record.status === 'Sending' : false;

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
                    if (wasSending) {
                        successMsg = wasActive ? 'Dağıtım duraklatıldı.' : 'Dağıtım sürdürüldü.';
                    } else {
                        successMsg = wasActive ? 'Bülten pasife alındı.' : 'Bülten aktife alındı.';
                    }
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

    function queueIssue(record) {
        var data = new FormData();
        data.append('id', record.id);
        data.append('__RequestVerificationToken', token());

        fetch('/NewsletterIssue/Queue', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: data
        })
        .then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
            if (r.ok) {
                toast('success', 'Dağıtıma verildi', 'Gönderim arka planda sürüyor; ilerleme listede güncellenecek.');
                reloadTable();
                return;
            }
            return r.text().then(function (body) {
                var serverMsg = '';
                try { serverMsg = JSON.parse(body).message || ''; } catch (e) { serverMsg = ''; }
                toast('error', 'Hata', serverMsg || 'Bülten dağıtıma verilemedi.');
            });
        })
        .catch(function () {
            toast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.');
        });
    }

    function confirmQueue(record) {
        var count = audience();
        ConfirmModal.open({
            id: record.id,
            recordLabel: 'Konu:',
            email: record.subject || '—',
            actionLabel: 'Dağıtıma Ver',
            actionVariant: 'success',
            note: count > 0
                ? count + ' doğrulanmış adrese gönderilecek. Alıcı listesi bu anda dondurulur; gönderilmiş posta geri alınamaz ve bülten bir daha düzenlenemez.'
                : 'Şu anda doğrulanmış abone görünmüyor; dağıtım büyük olasılıkla reddedilecek.',
            onConfirm: function () { queueIssue(record); }
        });
    }

    function openPreview(record) {
        DetailModal.close();
        HtmlPreviewModal.open(record.subject || 'Bülten Önizlemesi', record.body || '', '/Preview/NewsletterIssue/' + encodeURIComponent(record.id));
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
            subject: record.subject || '',
            body:    record.body    || ''
        });
    }

    function openTestModal(record) {
        FormModal.open(buildTestConfig(record.id, function () {
            toast('success', 'Gönderildi', 'Deneme postası yola çıktı.');
        }), {});
    }

    function sendingIds() {
        return readRows()
            .filter(function (r) { return r.status === 'Sending'; })
            .map(function (r) { return r.id; });
    }

    function paintProgress(id, data) {
        var cell = document.querySelector('[data-progress-for="' + id + '"]');
        if (!cell) return;

        var done = cell.querySelector('.nl-progress__done');
        var total = cell.querySelector('.nl-progress__total');
        if (done) done.textContent = data.sentCount;
        if (total) total.textContent = data.recipientCount;

        var note = cell.querySelector('.nl-progress__note');
        if (!note) return;
        var parts = [];
        if (data.failedCount > 0) parts.push('<span class="nl-progress__failed">' + data.failedCount + ' başarısız</span>');
        if (data.skippedCount > 0) parts.push('<span class="nl-progress__skipped">' + data.skippedCount + ' atlandı</span>');
        note.innerHTML = parts.join('');
    }

    function poll() {
        var ids = sendingIds();
        if (!ids.length) {
            stopPolling();
            return;
        }

        Promise.all(ids.map(function (id) {
            return fetch('/NewsletterIssue/Progress/' + id, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.ok ? r.json() : null; })
                .catch(function () { return null; });
        }))
        .then(function (results) {
            var finished = false;
            results.forEach(function (data, i) {
                if (!data) return;
                if (data.status !== 'Sending') { finished = true; return; }
                paintProgress(ids[i], data);
            });
            if (finished) reloadTable();
        });
    }

    function startPolling() {
        stopPolling();
        if (!sendingIds().length) return;
        _pollTimer = window.setInterval(poll, POLL_MS);
    }

    function stopPolling() {
        if (_pollTimer) {
            window.clearInterval(_pollTimer);
            _pollTimer = null;
        }
    }

    function bindAll() {
        var rows = readRows();

        function findRow(id) {
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].id === id) return rows[i];
            }
            return null;
        }

        function onRow(selector, handler) {
            document.querySelectorAll(selector).forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var record = findRow(parseInt(btn.dataset.id, 10));
                    if (record) handler(record);
                });
            });
        }

        onRow('.ft-view-btn', function (record) {
            DetailModal.open(NewsletterDetailConfig, record, function (key) {
                if (key === 'edit') { DetailModal.close(); openEditModal(record); }
                if (key === 'preview') { openPreview(record); }
                if (key === 'test') { DetailModal.close(); openTestModal(record); }
            });
        });

        onRow('.nl-edit-btn', openEditModal);
        onRow('.nl-preview-btn', openPreview);
        onRow('.nl-test-btn', openTestModal);
        onRow('.nl-queue-btn', confirmQueue);

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
                    if (record && record.status === 'Sending') note = 'Bu bültenin dağıtımı sürüyor; silmek dağıtımı durdurur.';
                } else if (action.indexOf('Restore') !== -1) {
                    actionLabel = 'Geri Yükle';
                    actionVariant = 'success';
                } else if (action.indexOf('ToggleActive') !== -1) {
                    var isActive = record ? record.isActive : false;
                    actionLabel = isActive ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isActive ? 'warning' : 'success';
                    if (record && record.status === 'Sending') {
                        note = isActive
                            ? 'Dağıtım duraklar. Kime gönderildiği kayıtlı olduğu için aktife alındığında kaldığı yerden sürer.'
                            : 'Dağıtım kaldığı yerden sürer; daha önce gönderilenlere ikinci kez gitmez.';
                    }
                } else {
                    actionLabel = 'İşlemi Gerçekleştir';
                    actionVariant = 'neutral';
                }

                ConfirmModal.open({
                    id: id,
                    recordLabel: 'Konu:',
                    email: record ? (record.subject || 'ID: ' + id) : '—',
                    actionLabel: actionLabel,
                    actionVariant: actionVariant,
                    note: note,
                    onConfirm: function () { submitAction(form, record); }
                });
            });
        });

        startPolling();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('ft:table-rendered', bindAll);
        bindAll();

        var addBtn = document.getElementById('newsletter-add-btn');
        if (addBtn) {
            addBtn.addEventListener('click', openCreateModal);
        }
    });

})();
