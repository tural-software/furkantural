(function () {
    'use strict';

    window.onCommentTurnstileSuccess = function (token) {
        var input = document.getElementById('commentTurnstileToken');
        if (input) input.value = token;
    };

    document.addEventListener('DOMContentLoaded', function () {
        var parentInput = document.getElementById('commentParentId');
        var banner = document.getElementById('commentReplyingTo');
        var bannerName = document.getElementById('commentReplyingName');
        var cancelBtn = document.getElementById('commentReplyCancel');
        var formWrap = document.getElementById('yorum-formu');
        var bodyInput = document.querySelector('#commentForm [name="Body"]');

        if (!parentInput || !banner || !bannerName) return;

        function setParent(id, name) {
            parentInput.value = id || '';
            if (id) {
                bannerName.textContent = name || 'Yorum';
                banner.hidden = false;
            } else {
                banner.hidden = true;
            }
        }

        document.querySelectorAll('.comment__reply').forEach(function (btn) {
            btn.addEventListener('click', function () {
                setParent(btn.dataset.replyTo, btn.dataset.replyName);
                if (formWrap) formWrap.scrollIntoView({ behavior: 'smooth', block: 'start' });
                if (bodyInput) bodyInput.focus({ preventScroll: true });
            });
        });

        if (cancelBtn) {
            cancelBtn.addEventListener('click', function () {
                setParent('', '');
                if (bodyInput) bodyInput.focus({ preventScroll: true });
            });
        }

        if (parentInput.value) {
            var active = document.querySelector('.comment__reply[data-reply-to="' + parentInput.value + '"]');
            setParent(parentInput.value, active ? active.dataset.replyName : null);
        }
    });
})();
