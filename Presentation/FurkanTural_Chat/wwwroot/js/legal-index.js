(function () {
    'use strict';

    var dizin = document.querySelector('.legal-index');
    var kart = document.querySelector('.legal-card');
    if (!dizin || !kart) { return; }

    var liste = dizin.querySelector('.legal-index-list');
    var basliklar = kart.querySelectorAll('h2');
    if (!liste || basliklar.length === 0) { return; }

    function kimlik(metin, sira) {
        var taban = metin
            .toLowerCase()
            .replace(/[çğıöşü]/g, function (c) { return { 'ç': 'c', 'ğ': 'g', 'ı': 'i', 'ö': 'o', 'ş': 's', 'ü': 'u' }[c]; })
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '');
        return 'bolum-' + (taban || sira);
    }

    Array.prototype.forEach.call(basliklar, function (baslik, i) {
        if (!baslik.id) { baslik.id = kimlik(baslik.textContent, i + 1); }

        var bag = document.createElement('a');
        bag.href = '#' + baslik.id;
        bag.textContent = baslik.textContent;

        var madde = document.createElement('li');
        madde.appendChild(bag);
        liste.appendChild(madde);
    });

    dizin.hidden = false;
})();
