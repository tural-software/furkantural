(function () {
    'use strict';

    var SYMBOLS = '!#$%()*+,-./:;=?@[]^_{|}~';
    var LOWER = 'abcdefghijklmnopqrstuvwxyz';
    var UPPER = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    var DIGITS = '0123456789';
    var ALL = LOWER + UPPER + DIGITS + SYMBOLS;
    var LENGTH = 12;

    function randomIndex(bound) {
        var limit = Math.floor(4294967296 / bound) * bound;
        var buffer = new Uint32Array(1);
        var value;
        do {
            window.crypto.getRandomValues(buffer);
            value = buffer[0];
        } while (value >= limit);
        return value % bound;
    }

    function pick(set) {
        return set.charAt(randomIndex(set.length));
    }

    function generate() {
        var chars = [pick(LOWER), pick(UPPER), pick(DIGITS), pick(SYMBOLS)];
        while (chars.length < LENGTH) {
            chars.push(pick(ALL));
        }
        for (var i = chars.length - 1; i > 0; i--) {
            var j = randomIndex(i + 1);
            var swap = chars[i];
            chars[i] = chars[j];
            chars[j] = swap;
        }
        return chars.join('');
    }

    function fill(input) {
        input.type = 'text';
        input.value = generate();
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('blur', { bubbles: true }));
        input.focus();
        input.setSelectionRange(0, input.value.length);
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-password-generate]');
        if (!button) return;

        var input = document.querySelector(button.getAttribute('data-password-generate'));
        if (!input) return;

        fill(input);

        var hint = button.getAttribute('data-password-hint');
        var target = hint ? document.querySelector(hint) : null;
        if (target) target.textContent = 'Parola üretildi; kaydetmeden önce bir yere not alın.';
    });

    window.ftPassword = { symbols: SYMBOLS, length: LENGTH, generate: generate };
})();
