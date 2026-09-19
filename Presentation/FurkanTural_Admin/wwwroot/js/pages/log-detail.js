/**
 * Log Detail Modal — config + page binding
 * READ-ONLY: no create/edit/delete actions
 *
 * Depends on: detail-modal.js (DetailModal, DmFmt)
 */
(function () {
    'use strict';

    var LogDetailConfig = {
        title: 'Log Detayı',
        description: 'Seçilen sistem logunun ayrıntıları',

        header: {
            icon: 'logs',
            idLabel: function (r) { return 'ID: <strong>' + r.id + '</strong>'; },
            badges: [
                {
                    label: function (r) { return r.level || '—'; },
                    variant: function (r) {
                        var lvl = (r.level || '').toLowerCase();
                        if (lvl === 'error') return 'danger';
                        if (lvl === 'warning') return 'warn';
                        if (lvl === 'info') return 'info';
                        return 'neutral';
                    }
                },
                {
                    label: function (r) { return r.project || '—'; },
                    variant: function () { return 'neutral'; }
                }
            ]
        },

        sections: [
            {
                columns: 2,
                fields: [
                    {
                        label: 'Proje',
                        icon: 'field-text',
                        value: function (r) { return r.project || '—'; }
                    },
                    {
                        label: 'Log Tarihi',
                        icon: 'calendar',
                        value: function (r) { return r.date ? DmFmt.date(r.date) : '—'; }
                    },
                    {
                        label: 'Seviye',
                        icon: 'field-text',
                        value: function (r) { return r.level || '—'; },
                        badgeVariant: function (r) {
                            var lvl = (r.level || '').toLowerCase();
                            if (lvl === 'error') return 'danger';
                            if (lvl === 'warning') return 'warn';
                            if (lvl === 'info') return 'info';
                            return 'neutral';
                        }
                    },
                    {
                        label: 'IP Adresi',
                        icon: 'field-text',
                        value: function (r) { return r.ipAddress || '—'; }
                    },
                    {
                        label: 'Yol',
                        icon: 'field-text',
                        value: function (r) { return r.path || '—'; }
                    }
                ]
            },
            {
                title: 'Mesaj',
                icon: 'field-text',
                columns: 1,
                fields: [
                    {
                        label: 'Mesaj',
                        icon: 'field-text',
                        value: function (r) { return r.message || '—'; }
                    }
                ]
            },
            {
                title: 'Ayrıntı (Stack Trace)',
                icon: 'database',
                columns: 1,
                fields: [
                    {
                        label: 'Detail',
                        icon: 'field-text',
                        isCode: true,
                        value: function (r) { return r.detail || '—'; }
                    }
                ]
            },
            DmAudit.section()
        ],

        actions: [
            { key: 'close', label: 'Kapat', variant: 'secondary' }
        ]
    };

    function readRows() {
        var el = document.getElementById('__log-rows-json');
        if (!el) return [];
        try { return JSON.parse(el.textContent || '[]'); } catch (e) { return []; }
    }

    function reloadTable() {
        FtList.reload();
    }

    function bindAll() {
        var rows = readRows();

        document.querySelectorAll('.ft-view-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var id = parseInt(btn.dataset.id, 10);
                var record = null;
                for (var i = 0; i < rows.length; i++) {
                    if (rows[i].id === id) { record = rows[i]; break; }
                }
                if (!record) return;
                DetailModal.open(LogDetailConfig, record, function () { });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('ft:table-rendered', bindAll);
        bindAll();
    });
})();
