(function () {
    'use strict';

    var dugme = document.querySelector('[data-offline-retry]');
    if (dugme) {
        dugme.addEventListener('click', function () { location.reload(); });
    }

    window.addEventListener('online', function () { location.reload(); });
})();
