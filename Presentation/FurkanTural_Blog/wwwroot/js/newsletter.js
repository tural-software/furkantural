(function () {
    'use strict';

    window.onTurnstileSuccess = function (token) {
        var input = document.getElementById('turnstileToken');
        if (input) input.value = token;
    };
})();
