(function () {
    'use strict';

    window.onCommentTurnstileSuccess = function (token) {
        var input = document.getElementById('commentTurnstileToken');
        if (input) input.value = token;
    };

    document.addEventListener('DOMContentLoaded', function () {
        var done = document.getElementById('commentDoneDialog');
        if (done && typeof done.showModal === 'function') {
            try {
                done.showModal();
                var adres = new URL(window.location.href);
                if (adres.searchParams.has('yorum')) {
                    adres.searchParams.delete('yorum');
                    window.history.replaceState({}, '', adres.pathname + adres.search + adres.hash);
                }
            } catch (e) { }

            var kapat = document.getElementById('commentDoneClose');
            if (kapat) kapat.addEventListener('click', function () { done.close(); });
        }

        var parentInput = document.getElementById('commentParentId');
        var compose = document.getElementById('commentCompose');
        var target = document.getElementById('commentTarget');
        var targetName = document.getElementById('commentTargetName');
        var targetDate = document.getElementById('commentTargetDate');
        var targetBody = document.getElementById('commentTargetBody');
        var cancelBtn = document.getElementById('commentReplyCancel');
        var formWrap = document.getElementById('yorum-formu');
        var bodyInput = document.querySelector('#commentForm [name="Body"]');

        if (!parentInput || !compose || !target) return;

        var kapanisSayaci = null;

        function sureMs() {
            return window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 340;
        }

        function doldur(kart) {
            if (!kart) return;
            var ad = kart.querySelector('.comment__author');
            var tarih = kart.querySelector('.comment__date');
            var govde = kart.querySelector('.comment__body');
            if (targetName) targetName.textContent = ad ? ad.textContent.trim() : '';
            if (targetDate) targetDate.textContent = tarih ? tarih.textContent.trim() : '';
            if (targetBody) targetBody.textContent = govde ? govde.textContent.trim() : '';
        }

        function ac(id, kart) {
            if (kapanisSayaci) { clearTimeout(kapanisSayaci); kapanisSayaci = null; }
            parentInput.value = id;
            doldur(kart);
            target.hidden = false;
            requestAnimationFrame(function () {
                requestAnimationFrame(function () { compose.classList.add('is-replying'); });
            });
        }

        function kapat() {
            parentInput.value = '';
            compose.classList.remove('is-replying');
            if (kapanisSayaci) clearTimeout(kapanisSayaci);
            kapanisSayaci = setTimeout(function () {
                target.hidden = true;
                kapanisSayaci = null;
            }, sureMs());
        }

        document.querySelectorAll('.comment__reply').forEach(function (btn) {
            btn.addEventListener('click', function () {
                ac(btn.dataset.replyTo, btn.closest('.comment__card'));
                if (formWrap) formWrap.scrollIntoView({ behavior: 'smooth', block: 'start' });
                if (bodyInput) bodyInput.focus({ preventScroll: true });
            });
        });

        if (cancelBtn) {
            cancelBtn.addEventListener('click', function () {
                kapat();
                if (bodyInput) bodyInput.focus({ preventScroll: true });
            });
        }

        if (parentInput.value) {
            var aktif = document.querySelector('.comment__reply[data-reply-to="' + parentInput.value + '"]');
            ac(parentInput.value, aktif ? aktif.closest('.comment__card') : null);
        }
    });
})();
