/**
 * ChatMessage Index — detail modal (read-only, audio playback) + toggle/restore
 * Depends on: detail-modal.js, confirm-modal.js
 */
(function () {
    'use strict';

    function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }
    function attachmentSrc(r) {
        // Chat ekleri API'de statik sunulmaz; admin oturum token'ıyla sunucu taraflı proxy'den akar.
        var rel = (r.attachmentUrl || '').replace(/^\//, '');
        return '/ChatMessage/Attachment?file=' + encodeURIComponent(rel);
    }

    // Panel ikonlarıyla tutarlı SVG'ler tek kaynaktan (window.__ICONS / IconLibrary.cs).
    var MIC_SVG = (window.__ICONS || {})['mic'] || '';
    var IMG_SVG = (window.__ICONS || {})['image'] || '';
    function typeLabel(t) {
        t = (t || '').toLowerCase();
        return t === 'audio' ? 'Ses Kaydı' : t === 'image' ? 'Görsel' : t === 'video' ? 'Video' : 'Metin';
    }
    function userLabel(name, id) { return name ? esc(name) : ('#' + id); }

    var MessageDetailConfig = {
        title: 'Mesaj Detayı',
        description: 'Seçilen mesaj kaydına ait detaylar',
        header: {
            icon: 'messages',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                { label: function (r) { return r.isRead ? '✓ Okundu' : 'Okunmadı'; }, variant: function (r) { return r.isRead ? 'success' : 'neutral'; } },
                { label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; }, variant: function (r) { return r.isActive ? 'success' : 'danger'; } },
                { label: function (r) { return 'Silinmiş: ' + (r.isDeleted ? 'Evet' : 'Hayır'); }, variant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; } }
            ]
        },
        sections: [
            {
                columns: 2,
                fields: [
                    { label: 'Gönderen', icon: 'user', html: true, value: function (r) { return userLabel(r.senderUsername, r.senderId); } },
                    { label: 'Alıcı', icon: 'user', html: true, value: function (r) { return userLabel(r.receiverUsername, r.receiverId); } },
                    { label: 'Tür', icon: 'field-text', value: function (r) { return typeLabel(r.messageType); } },
                    { label: 'Okundu', icon: 'check-circle', value: function (r) { return r.isRead ? 'Evet' : 'Hayır'; } },
                    { label: 'Okunma Tarihi', icon: 'calendar', value: function (r) { return r.readAt ? DmFmt.date(r.readAt) : '—'; } },
                    { label: 'Oluşturulma Tarihi', icon: 'calendar', value: function (r) { return DmFmt.date(r.createdAt); } }
                ]
            },
            {
                title: 'İçerik',
                icon: 'field-text',
                columns: 1,
                fields: [
                    {
                        label: 'Mesaj',
                        icon: 'field-text',
                        html: true,
                        value: function (r) {
                            var type = (r.messageType || '').toLowerCase();
                            if (type === 'audio') {
                                var cap = '<div class="dm-media-cap">' + MIC_SVG + ' Ses Kaydı</div>';
                                return cap + (r.attachmentUrl ? '<audio controls preload="metadata" src="' + attachmentSrc(r) + '" style="max-width:100%"></audio>' : '');
                            }
                            if (type === 'image') {
                                var capI = '<div class="dm-media-cap">' + IMG_SVG + ' Görsel</div>';
                                return capI + (r.attachmentUrl ? '<img src="' + attachmentSrc(r) + '" alt="Görsel" loading="lazy" style="max-width:100%;max-height:320px;border-radius:8px">' : '');
                            }
                            if (type === 'video') {
                                var capV = '<div class="dm-media-cap">' + IMG_SVG + ' Video</div>';
                                return capV + (r.attachmentUrl ? '<video controls preload="metadata" src="' + attachmentSrc(r) + '" style="max-width:100%;max-height:320px;border-radius:8px"></video>' : '');
                            }
                            return r.content ? esc(r.content) : '—';
                        }
                    }
                ]
            }
        ],
        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__message-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__messageMeta || {};
        var params = new URLSearchParams({
            search: meta.search || '', usernameFilter: meta.usernameFilter || '', typeFilter: meta.typeFilter || '',
            activeFilter: meta.activeFilter || '', deletedFilter: meta.deletedFilter || '',
            dateFrom: meta.dateFrom || '', dateTo: meta.dateTo || '',
            pageNumber: meta.pageNumber || 1, pageSize: meta.pageSize || 10
        });
        fetch('/ChatMessage/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { if (!r.ok) throw new Error('TablePartial ' + r.status); return r.text(); })
            .then(function (html) { var s = document.getElementById('message-table-section'); if (!s) return; s.innerHTML = html; bindAll(); })
            .catch(function (err) { console.error('reloadTable hatası:', err); });
    }

    function submitAction(form, isActive, actionKey) {
        var data = new FormData(form);
        fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: data })
            .then(function (r) {
                if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
                if (r.ok) {
                    var msg = actionKey === 'Restore' ? 'Geri yükleme işlemi başarılı.' : (isActive ? 'Kayıt pasife alındı.' : 'Kayıt aktife alındı.');
                    if (typeof showToast === 'function') showToast('success', 'Başarılı', msg);
                    reloadTable();
                } else {
                    return r.text().then(function (body) {
                        var serverMsg = ''; try { serverMsg = JSON.parse(body).message || ''; } catch (e) { }
                        if (typeof showToast === 'function') showToast('error', 'Hata', serverMsg || 'İşlem başarısız oldu.');
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
                if (rec) DetailModal.open(MessageDetailConfig, rec, function () { });
            });
        });
        document.querySelectorAll('.row-actions form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var id = parseInt((form.querySelector('input[name="id"]') || {}).value, 10), rec = null;
                for (var i = 0; i < rows.length; i++) { if (rows[i].id === id) { rec = rows[i]; break; } }
                var action = form.action || '', actionLabel, actionVariant, actionKey;
                if (action.indexOf('Restore') !== -1) { actionKey = 'Restore'; actionLabel = 'Geri Yükle'; actionVariant = 'success'; }
                else { actionKey = 'ToggleActive'; var a = rec ? rec.isActive : false; actionLabel = a ? 'Pasife Al' : 'Aktife Al'; actionVariant = a ? 'warning' : 'success'; }
                var capturedIsActive = rec ? rec.isActive : false;
                var recordName = rec ? ('#' + rec.senderId + ' → #' + rec.receiverId) : ('ID: ' + id);
                ConfirmModal.open({ id: id, email: recordName, actionLabel: actionLabel, actionVariant: actionVariant, onConfirm: function () { submitAction(form, capturedIsActive, actionKey); } });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () { bindAll(); });
})();
