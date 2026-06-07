// Ortak zaman gösterimi — API'den gelen UTC ISO ('Z') değerlerini DAİMA Europe/Istanbul'da biçimler.
// Tek kural: saklama UTC, gösterim Türkiye. Cihaz saat diliminden bağımsızdır.
(function () {
    'use strict';
    var TZ = 'Europe/Istanbul';

    function parse(v) { if (!v) return null; var d = new Date(v); return isNaN(d.getTime()) ? null : d; }

    function dateTime(v) {
        var d = parse(v); if (!d) return '—';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', timeZone: TZ })
             + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: TZ });
    }
    function date(v) {
        var d = parse(v); if (!d) return '—';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric', timeZone: TZ });
    }
    function time(v) {
        var d = parse(v); if (!d) return '';
        return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: TZ });
    }
    function relative(v) {
        var d = parse(v); if (!d) return '';
        var s = Math.floor((Date.now() - d.getTime()) / 1000);
        if (s < 0) s = 0;
        if (s < 60) return 'az önce';
        var m = Math.floor(s / 60); if (m < 60) return m + ' dk önce';
        var h = Math.floor(m / 60); if (h < 24) return h + ' sa önce';
        var days = Math.floor(h / 24);
        if (days === 1) return 'dün';
        if (days < 7) return days + ' gün önce';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', timeZone: TZ });
    }
    // Tarih girişleri (yyyy-MM-dd) için Istanbul gününü döndürür.
    function dateInput(v) {
        var d = parse(v); if (!d) return '';
        return d.toLocaleDateString('en-CA', { timeZone: TZ }); // "YYYY-MM-DD"
    }

    window.FtTime = { dateTime: dateTime, date: date, time: time, relative: relative, dateInput: dateInput, tz: TZ };
})();
