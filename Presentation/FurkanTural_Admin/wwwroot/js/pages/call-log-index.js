/**
 * CallLog Index — detail modal (read-only) + toggle/restore
 * Depends on: detail-modal.js, confirm-modal.js
 */
(function () {
    'use strict';

    function fmtDur(s) { return (s && s > 0) ? (Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0')) : '—'; }
    function typeLabel(t) { return (String(t || '').toLowerCase() === 'video') ? 'Görüntülü' : 'Sesli'; }

    var statusVariant = {
        answered: 'success', ended: 'neutral', ringing: 'warning',
        missed: 'danger', rejected: 'danger', canceled: 'neutral', failed: 'danger'
    };

    var CallDetailConfig = {
        title: 'Arama Detayı',
        description: 'Seçilen arama kaydına ait detaylar',
        header: {
            icon: 'calls',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                { label: function (r) { return r.status || '—'; }, variant: function (r) { return statusVariant[String(r.status || '').toLowerCase()] || 'neutral'; } },
                { label: function (r) { return typeLabel(r.callType); }, variant: function () { return 'neutral'; } },
                { label: function (r) { return r.isActive ? '✓ Aktif' : '⊘ Pasif'; }, variant: function (r) { return r.isActive ? 'success' : 'danger'; } },
                { label: function (r) { return 'Silinmiş: ' + (r.isDeleted ? 'Evet' : 'Hayır'); }, variant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; } }
            ]
        },
        sections: [
            {
                columns: 2,
                fields: [
                    { label: 'Arayan', icon: 'user', value: function (r) { return (r.callerName || '#' + r.callerId); } },
                    { label: 'Aranan', icon: 'user', value: function (r) { return (r.calleeName || '#' + r.calleeId); } },
                    { label: 'Tür', icon: 'field-text', value: function (r) { return typeLabel(r.callType); } },
                    { label: 'Durum', icon: 'field-text', value: function (r) { return r.status || '—'; } },
                    { label: 'Süre', icon: 'calendar', value: function (r) { return fmtDur(r.durationSeconds); } },
                    { label: 'Başlangıç', icon: 'calendar', value: function (r) { return DmFmt.date(r.startedAt); } },
                    { label: 'Cevaplanma', icon: 'calendar', value: function (r) { return r.answeredAt ? DmFmt.date(r.answeredAt) : '—'; } },
                    { label: 'Bitiş', icon: 'calendar', value: function (r) { return r.endedAt ? DmFmt.date(r.endedAt) : '—'; } }
                ]
            },
            DmAudit.section()
        ],
        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__call-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        var meta = window.__callMeta || {};
        var params = new URLSearchParams({
            search: meta.search || '', typeFilter: meta.typeFilter || '', statusFilter: meta.statusFilter || '',
            activeFilter: meta.activeFilter || '', deletedFilter: meta.deletedFilter || '',
            dateFrom: meta.dateFrom || '', dateTo: meta.dateTo || '',
            pageNumber: meta.pageNumber || 1, pageSize: meta.pageSize || 10
        });
        fetch('/CallLog/TablePartial?' + params.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { if (!r.ok) throw new Error('TablePartial ' + r.status); return r.text(); })
            .then(function (html) { var s = document.getElementById('call-table-section'); if (!s) return; s.innerHTML = html; bindAll(); })
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
                if (rec) DetailModal.open(CallDetailConfig, rec, function () { });
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
                var recordName = rec ? ((rec.callerName || '#' + rec.callerId) + ' → ' + (rec.calleeName || '#' + rec.calleeId)) : ('ID: ' + id);
                ConfirmModal.open({ id: id, email: recordName, actionLabel: actionLabel, actionVariant: actionVariant, onConfirm: function () { submitAction(form, capturedIsActive, actionKey); } });
            });
        });
    }

    // Arama bit hızı politikası ayar formu (tablo dışı, tek sefer bağlanır)
    function bindSettings() {
        var form = document.getElementById('callSettingsForm');
        if (!form) return;
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var data = new FormData(form);
            fetch(form.action, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: data })
                .then(function (r) {
                    if (r.status === 401) { window.location.href = '/Auth/Login'; return; }
                    if (r.ok) {
                        if (typeof showToast === 'function') showToast('success', 'Başarılı', 'Arama politikası kaydedildi. Sonraki aramalarda geçerli olacak.');
                    } else {
                        return r.text().then(function (body) {
                            var msg = ''; try { msg = JSON.parse(body).message || ''; } catch (e) { }
                            if (typeof showToast === 'function') showToast('error', 'Hata', msg || 'Ayar kaydedilemedi.');
                        });
                    }
                })
                .catch(function () { if (typeof showToast === 'function') showToast('error', 'Hata', 'Sunucudan beklenmeyen bir hata döndü.'); });
        });
    }

    document.addEventListener('DOMContentLoaded', function () { bindAll(); bindSettings(); });
})();
