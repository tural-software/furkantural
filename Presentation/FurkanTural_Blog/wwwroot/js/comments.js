(function () {
    'use strict';

    window.onCommentTurnstileSuccess = function (token) {
        var input = document.getElementById('commentTurnstileToken');
        if (input) input.value = token;
    };

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('commentForm');
        var done = document.getElementById('commentDoneDialog');
        var alertSlot = document.getElementById('commentAlert');
        var tokenInput = document.getElementById('commentTurnstileToken');
        var parentInput = document.getElementById('commentParentId');
        var compose = document.getElementById('commentCompose');
        var target = document.getElementById('commentTarget');
        var targetName = document.getElementById('commentTargetName');
        var targetDate = document.getElementById('commentTargetDate');
        var targetBody = document.getElementById('commentTargetBody');
        var cancelBtn = document.getElementById('commentReplyCancel');
        var formWrap = document.getElementById('yorum-formu');
        var bodyInput = document.querySelector('#commentForm [name="Body"]');
        var serverAlert = document.querySelector('.comment-form-wrap > .comment-alert');

        function adresiTemizle() {
            try {
                var adres = new URL(window.location.href);
                if (adres.searchParams.has('yorum')) {
                    adres.searchParams.delete('yorum');
                    window.history.replaceState({}, '', adres.pathname + adres.search + adres.hash);
                }
            } catch (e) { }
        }

        function onayiGoster() {
            if (!done || typeof done.showModal !== 'function') return;
            try { done.showModal(); } catch (e) { }
        }

        if (done) {
            var kapatDugmesi = document.getElementById('commentDoneClose');
            if (kapatDugmesi) kapatDugmesi.addEventListener('click', function () { done.close(); });

            if (done.dataset.autoOpen === 'true') {
                onayiGoster();
                adresiTemizle();
            }
        }

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
            if (!parentInput || !compose || !target) return;
            if (kapanisSayaci) { clearTimeout(kapanisSayaci); kapanisSayaci = null; }
            parentInput.value = id;
            doldur(kart);
            target.hidden = false;
            requestAnimationFrame(function () {
                requestAnimationFrame(function () { compose.classList.add('is-replying'); });
            });
        }

        function kapat() {
            if (!parentInput || !compose || !target) return;
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

        if (parentInput && parentInput.value) {
            var aktif = document.querySelector('.comment__reply[data-reply-to="' + parentInput.value + '"]');
            ac(parentInput.value, aktif ? aktif.closest('.comment__card') : null);
        }

        if (!form || !window.fetch || !window.FormData) return;

        var submitBtn = form.querySelector('.comment-form__submit');
        var submitMetni = submitBtn ? submitBtn.textContent : '';
        var gonderiliyor = false;

        function hatalariTemizle() {
            form.querySelectorAll('[data-error-for]').forEach(function (kutu) {
                kutu.textContent = '';
            });
        }

        function alanHatasi(ad, mesaj) {
            var kutu = form.querySelector('[data-error-for="' + ad + '"]');
            if (kutu) kutu.textContent = mesaj || '';
        }

        function uyari(mesaj, iyi) {
            if (!alertSlot) return;
            alertSlot.textContent = '';
            if (!mesaj) return;
            var p = document.createElement('p');
            p.className = iyi ? 'comment-alert comment-alert--ok' : 'comment-alert comment-alert--warn';
            p.textContent = mesaj;
            alertSlot.appendChild(p);
        }

        function turnstileSifirla() {
            if (tokenInput) tokenInput.value = '';
            var widget = form.querySelector('.cf-turnstile');
            if (!widget || !window.turnstile || typeof window.turnstile.reset !== 'function') return;
            try { window.turnstile.reset(widget); } catch (e) { }
        }

        function basari(mesaj) {
            if (bodyInput) bodyInput.value = '';
            kapat();
            uyari(mesaj || 'Yorumunuz alındı. Onaylandıktan sonra sayfada görünecek.', true);
            onayiGoster();
        }

        function basarisiz(sonuc) {
            var hatalar = sonuc && sonuc.errors;
            var ilkAlan = null;

            if (hatalar) {
                Object.keys(hatalar).forEach(function (anahtar) {
                    var ad = anahtar.indexOf('.') >= 0 ? anahtar.slice(anahtar.lastIndexOf('.') + 1) : anahtar;
                    alanHatasi(ad, hatalar[anahtar]);
                    if (!ilkAlan) ilkAlan = form.querySelector('[name="' + ad + '"]');
                });
            }

            uyari((sonuc && sonuc.message) || 'Yorum şu anda alınamıyor. Kısa süre sonra tekrar deneyin.', false);

            if (ilkAlan && typeof ilkAlan.focus === 'function') ilkAlan.focus();
            else if (alertSlot) alertSlot.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }

        function bitir() {
            gonderiliyor = false;
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = submitMetni;
            }
            turnstileSifirla();
        }

        form.addEventListener('submit', function (olay) {
            if (gonderiliyor) {
                olay.preventDefault();
                return;
            }

            olay.preventDefault();
            gonderiliyor = true;

            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = 'Gönderiliyor…';
            }

            if (serverAlert && serverAlert.parentNode) {
                serverAlert.parentNode.removeChild(serverAlert);
                serverAlert = null;
            }

            hatalariTemizle();
            uyari('');

            fetch(form.action, {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
                body: new FormData(form),
                credentials: 'same-origin'
            }).then(function (yanit) {
                if (!yanit.ok) throw new Error(String(yanit.status));
                return yanit.json();
            }).then(function (sonuc) {
                if (sonuc && sonuc.ok) basari(sonuc.message);
                else basarisiz(sonuc);
            }).catch(function () {
                uyari('Yorum şu anda gönderilemedi. Bağlantınızı denetleyip tekrar deneyin.', false);
            }).then(bitir);
        });
    });
})();
