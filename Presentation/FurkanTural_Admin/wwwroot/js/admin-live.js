(function () {
    'use strict';

    var HUB = '/bff/hubs/admin';
    var RETRY_MS = 3000;

    var KINDS = {
        comment: { label: 'yorum', controller: 'Comment', field: 'comments' },
        contact: { label: 'iletişim mesajı', controller: 'Contact', field: 'contacts' },
        report: { label: 'şikayet', controller: 'Report', field: 'reports' }
    };

    var badge = null;
    var badgeCount = null;
    var notice = null;
    var pendingSince = 0;
    var live = null;

    function gorunenListe() {
        var section = document.querySelector('[data-list-controller]');
        return section ? section.dataset.listController : null;
    }

    function sayi(value) {
        return (value || 0).toLocaleString('tr-TR');
    }

    function rozetiYaz(total) {
        if (!badge || !badgeCount) return;
        badgeCount.textContent = sayi(total);
        badge.hidden = !(total > 0);
    }

    function panoyuYaz(payload) {
        var kpi = document.querySelector('[data-kpi="open-work"]');
        if (!kpi) return;

        var value = kpi.querySelector('.kpi__value');
        if (value) value.textContent = sayi(payload.total);

        var detail = kpi.querySelector('.kpi__detail');
        if (detail) {
            detail.textContent = sayi(payload.contacts) + ' okunmamış mesaj · '
                + sayi(payload.reports) + ' bekleyen şikayet · '
                + sayi(payload.comments) + ' bekleyen yorum';
        }
    }

    function seridiKaldir() {
        if (notice && notice.parentNode) notice.parentNode.removeChild(notice);
        notice = null;
        pendingSince = 0;
    }

    function seridiGoster(kind, adet) {
        var section = document.querySelector('[data-list-controller]');
        if (!section) return;

        var tanim = KINDS[kind];
        if (!tanim || tanim.controller !== section.dataset.listController) return;

        if (!notice) {
            notice = document.createElement('div');
            notice.className = 'live-notice';
            notice.setAttribute('role', 'status');

            var metin = document.createElement('span');
            metin.className = 'live-notice__text';
            notice.appendChild(metin);

            var dugme = document.createElement('button');
            dugme.type = 'button';
            dugme.className = 'live-notice__action';
            dugme.textContent = 'Göster';
            dugme.addEventListener('click', function () {
                seridiKaldir();
                if (window.FtList) FtList.reload();
            });
            notice.appendChild(dugme);

            section.parentNode.insertBefore(notice, section);
        }

        pendingSince += adet;
        notice.querySelector('.live-notice__text').textContent =
            pendingSince + ' yeni ' + tanim.label + ' geldi.';
    }

    function olayGeldi(payload) {
        if (!payload) return;

        var onceki = parseInt(((badgeCount && badgeCount.textContent) || '0').replace(/\D/g, ''), 10) || 0;
        var total = payload.total || 0;
        rozetiYaz(total);
        panoyuYaz(payload);

        var tanim = KINDS[payload.kind];
        if (!tanim) return;

        var artis = total - onceki;
        seridiGoster(payload.kind, artis > 0 ? artis : 1);

        if (tanim.controller === gorunenListe() && window.FtList && typeof FtList.refreshStats === 'function') {
            FtList.refreshStats();
        }
    }

    function esitle() {
        if (!live || !window.signalR || live.state !== signalR.HubConnectionState.Connected) return;
        live.invoke('RefreshPendingWork').catch(function () { });
    }

    function baglan() {
        if (!window.signalR) return;

        var connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB)
            .withAutomaticReconnect()
            .build();

        live = connection;
        connection.on('PendingWorkChanged', olayGeldi);

        connection.onclose(function () {
            window.setTimeout(function () { baslat(connection); }, RETRY_MS);
        });

        baslat(connection);
    }

    function baslat(connection) {
        connection.start().catch(function (error) {
            if (error && /401/.test(String(error))) {
                window.location.href = '/Auth/Login';
                return;
            }
            window.setTimeout(function () { baslat(connection); }, RETRY_MS);
        });
    }

    function init() {
        badge = document.getElementById('adminPendingBadge');
        badgeCount = document.getElementById('adminPendingCount');
        if (!badge) return;

        document.addEventListener('ft:table-rendered', function () {
            seridiKaldir();
            esitle();
        });
        baglan();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
