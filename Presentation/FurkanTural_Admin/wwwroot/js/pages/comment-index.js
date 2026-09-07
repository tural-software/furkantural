(function () {
    'use strict';

    function statusLabel(r) {
        if (r.status === 'Approved') return '✓ Onaylı';
        if (r.status === 'Rejected') return '⊘ Reddedildi';
        return '⏱ Bekliyor';
    }

    function statusVariant(r) {
        if (r.status === 'Approved') return 'success';
        if (r.status === 'Rejected') return 'danger';
        return 'warning';
    }

    var CommentDetailConfig = {
        title: 'Yorum Detayı',
        description: 'Seçilen yorumun tamamı ve denetim bilgileri',

        header: {
            icon: 'comments',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                { label: statusLabel, variant: statusVariant },
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
                fields: [
                    {
                        label: 'Yorum',
                        icon: 'field-text',
                        multiline: true,
                        value: function (r) { return r.body || '—'; }
                    }
                ]
            },
            {
                columns: 2,
                fields: [
                    {
                        label: 'Yazan',
                        icon: 'user',
                        value: function (r) {
                            return (r.authorName || '—') + (r.createdBy ? ' (yazının sahibi)' : '');
                        }
                    },
                    {
                        label: 'E-posta',
                        icon: 'mail',
                        isCode: true,
                        value: function (r) { return r.authorEmail || '—'; }
                    },
                    {
                        label: 'Yazı',
                        icon: 'blogs',
                        value: function (r) { return r.blogTitle || ('#' + r.blogId); }
                    },
                    {
                        label: 'Yanıtladığı Yorum',
                        icon: 'reply',
                        value: function (r) {
                            return r.parentId
                                ? (r.parentAuthorName || 'ID: ' + r.parentId) + ' (#' + r.parentId + ')'
                                : 'Yazıya doğrudan bırakıldı';
                        }
                    },
                    {
                        label: 'Gelen Yanıt',
                        icon: 'field-number',
                        value: function (r) { return r.replyCount > 0 ? r.replyCount + ' yanıt' : 'Yanıt yok'; },
                        badgeVariant: function (r) { return r.replyCount > 0 ? 'primary' : 'neutral'; }
                    },
                    {
                        label: 'Yanıt Bildirimi',
                        icon: 'field-bool',
                        value: function (r) {
                            return r.notifyOnReply
                                ? 'Açık — yanıt yazılırsa adrese posta gider'
                                : 'Kapalı — yazılan yanıt duyurulmaz';
                        },
                        badgeVariant: function (r) { return r.notifyOnReply ? 'success' : 'neutral'; }
                    },
                    {
                        label: 'Onay Tarihi',
                        icon: 'calendar',
                        value: function (r) {
                            return r.approvedAt ? new Date(r.approvedAt).toLocaleString('tr-TR') : 'Henüz onaylanmadı';
                        }
                    }
                ]
            },
            DmAudit.section()
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' },
            {
                key: 'reply', label: 'Yanıtla', icon: 'reply', variant: 'primary',
                hidden: function (r) { return r.isDeleted || r.status !== 'Approved' || r.parentId; }
            }
        ]
    };

    function buildReplyConfig(record, onSuccess) {
        return {
            title: 'Yoruma Yanıt Ver',
            description: (record.authorName || 'Yorum') + ' kişisinin yorumuna yazının sahibi olarak yanıt verirsiniz.',
            submitUrl: '/Comment/Reply/' + record.id,
            submitLabel: 'Yanıtla',
            fields: [
                {
                    name: 'body',
                    label: 'Yanıt',
                    type: 'textarea',
                    required: true,
                    maxLength: 4000,
                    rows: 6,
                    placeholder: 'Yanıtınızı yazın…',
                    helpText: record.notifyOnReply
                        ? 'Yanıt beklemeden yayına girer ve yorumu bırakan kişiye posta ile bildirilir.'
                        : 'Yanıt beklemeden yayına girer. Bu kişi bildirim istemediği için posta gönderilmez.'
                }
            ],
            onSuccess: onSuccess
        };
    }

    function readRows() {
        var el = document.getElementById('__comment-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__commentMeta || {};
        var params = new URLSearchParams({
            name:          meta.name          || '',
            statusFilter:  meta.statusFilter  || '',
            activeFilter:  meta.activeFilter  || '',
            deletedFilter: meta.deletedFilter || '',
            dateFrom:      meta.dateFrom      || '',
            dateTo:        meta.dateTo        || '',
            pageNumber:    meta.pageNumber    || 1,
            pageSize:      meta.pageSize      || 10
        });

        fetch('/Comment/TablePartial?' + params.toString(), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('TablePartial ' + r.status);
            return r.text();
        })
        .then(function (html) {
            var section = document.getElementById('comment-table-section');
            if (!section) return;
            section.innerHTML = html;
            bindAll();
            document.dispatchEvent(new CustomEvent('ft:table-rendered'));
        })
        .catch(function (err) {
            console.error('reloadTable hatası:', err);
        });
    }

    var ACTION_MESSAGES = {
        Delete:       { success: 'Silme işlemi başarılı.',        error: 'Silme işlemi başarısız oldu.' },
        Restore:      { success: 'Geri yükleme işlemi başarılı.', error: 'Geri yükleme işlemi başarısız oldu.' },
        ToggleActive: { success: null,                            error: 'Durum değiştirme işlemi başarısız oldu.' },
        SetStatus:    { success: null,                            error: 'Yorum durumu değiştirilemedi.' },
        Reply:        { success: 'Yanıtınız yayına girdi.',       error: 'Yanıt gönderilemedi.' }
    };

    function resolveActionKey(actionUrl) {
        if (actionUrl.indexOf('SetStatus')    !== -1) return 'SetStatus';
        if (actionUrl.indexOf('Delete')       !== -1) return 'Delete';
        if (actionUrl.indexOf('Restore')      !== -1) return 'Restore';
        if (actionUrl.indexOf('ToggleActive') !== -1) return 'ToggleActive';
        return null;
    }

    function toast(kind, title, message) {
        if (typeof showToast === 'function') showToast(kind, title, message);
    }

    function submitAction(form, context) {
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
                    successMsg = context.isActive ? 'Yorum pasife alındı.' : 'Yorum aktife alındı.';
                } else if (actionKey === 'SetStatus') {
                    successMsg = context.status === 'approved'
                        ? 'Yorum yayına alındı.'
                        : 'Yorum reddedildi ve yayından kaldırıldı.';
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

    function openReplyModal(record) {
        FormModal.open(buildReplyConfig(record, function () {
            toast('success', 'Başarılı', ACTION_MESSAGES.Reply.success);
            reloadTable();
        }), { body: '' });
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

                DetailModal.open(CommentDetailConfig, record, function (key) {
                    if (key === 'reply') {
                        DetailModal.close();
                        openReplyModal(record);
                    }
                });
            });
        });

        document.querySelectorAll('.ft-reply-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var record = findRow(parseInt(btn.dataset.id, 10));
                if (record) openReplyModal(record);
            });
        });

        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();

                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10);
                var record = findRow(id);
                var action = form.action || '';
                var statusInput = form.querySelector('input[name="status"]');
                var wantedStatus = statusInput ? statusInput.value : null;
                var actionLabel, actionVariant, note;

                if (action.indexOf('SetStatus') !== -1) {
                    if (wantedStatus === 'approved') {
                        actionLabel = 'Yayına Al';
                        actionVariant = 'success';
                        if (record && record.parentId && record.notifyOnReply === false) {
                            note = 'Bu bir yanıt. Yanıtlanan kişi bildirim istemişse onaydan sonra kendisine posta gider.';
                        }
                    } else {
                        actionLabel = 'Reddet';
                        actionVariant = 'warning';
                        note = 'Yorum yayından kalkar ama silinmez; kararı sonradan değiştirebilirsiniz.';
                    }
                } else if (action.indexOf('Delete') !== -1) {
                    actionLabel = 'Kaydı Sil';
                    actionVariant = 'danger';
                    if (record && record.replyCount > 0) {
                        note = 'Bu yoruma ' + record.replyCount + ' yanıt verilmiş; silinince o yanıtlar bağlamsız kalır.';
                    }
                } else if (action.indexOf('Restore') !== -1) {
                    actionLabel = 'Geri Yükle';
                    actionVariant = 'success';
                } else if (action.indexOf('ToggleActive') !== -1) {
                    var isActive = record ? record.isActive : false;
                    actionLabel = isActive ? 'Pasife Al' : 'Aktife Al';
                    actionVariant = isActive ? 'warning' : 'success';
                    if (isActive && record && record.status === 'Approved') {
                        note = 'Onaylı olsa da pasife alınan yorum sayfada görünmez.';
                    }
                } else {
                    actionLabel = 'İşlemi Gerçekleştir';
                    actionVariant = 'neutral';
                }

                var context = {
                    isActive: record ? record.isActive : false,
                    status: wantedStatus
                };

                ConfirmModal.open({
                    id: id,
                    recordLabel: 'Yorum:',
                    email: record ? (record.authorName || 'ID: ' + id) : '—',
                    actionLabel: actionLabel,
                    actionVariant: actionVariant,
                    note: note,
                    onConfirm: function () { submitAction(form, context); }
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('ft:table-reload', reloadTable);
        bindAll();
    });

})();
