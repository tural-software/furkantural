(function () {
    'use strict';

    var HUB = '/bff/hubs/admin';
    var RETRY_MS = 3000;

    var KINDS = {
        comment: { label: 'yorum', controller: 'Comment' },
        contact: { label: 'iletişim mesajı', controller: 'Contact' },
        report: { label: 'şikayet', controller: 'Report' },
        user: { label: 'kullanıcı', controller: 'User' },
        friend: { label: 'arkadaşlık kaydı', controller: 'UserFriend' },
        message: { label: 'mesaj', controller: 'ChatMessage' },
        call: { label: 'arama kaydı', controller: 'CallLog' },
        subscriber: { label: 'abone', controller: 'Subscriber' },
        newsletter: { label: 'bülten', controller: 'NewsletterIssue' }
    };

    var badge = null;
    var badgeCount = null;
    var notice = null;
    var noticeAdded = 0;
    var myId = 0;
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
        noticeAdded = 0;
    }

    function seridiGoster(tanim, added) {
        var section = document.querySelector('[data-list-controller]');
        if (!section) return;

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

        noticeAdded += added;
        notice.querySelector('.live-notice__text').textContent = noticeAdded > 0
            ? sayi(noticeAdded) + ' yeni ' + tanim.label + ' geldi.'
            : 'Listede güncellenen kayıtlar var.';
    }

    function oturumBasladi(payload) {
        myId = payload && payload.userId ? payload.userId : 0;
    }

    function bekleyenIsDegisti(payload) {
        if (!payload) return;
        rozetiYaz(payload.total || 0);
        panoyuYaz(payload);
    }

    function listelerDegisti(payload) {
        if (!payload || !payload.changes) return;

        var controller = gorunenListe();
        if (!controller) return;

        var tanim = null;
        var added = 0;
        var changed = 0;

        payload.changes.forEach(function (change) {
            var aday = KINDS[change.kind];
            if (!aday || aday.controller !== controller) return;

            tanim = aday;
            if (change.actorId && change.actorId === myId) return;

            added += change.added || 0;
            changed += change.changed || 0;
        });

        if (!tanim) return;

        if (window.FtList && typeof FtList.refreshStats === 'function') {
            FtList.refreshStats();
        }

        if (added > 0 || changed > 0) {
            seridiGoster(tanim, added);
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
        connection.on('AdminSession', oturumBasladi);
        connection.on('PendingWorkChanged', bekleyenIsDegisti);
        connection.on('ListsChanged', listelerDegisti);

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
