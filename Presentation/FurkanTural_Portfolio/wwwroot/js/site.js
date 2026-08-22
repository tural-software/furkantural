/* ================================================
   Furkan Tural Portfolio — site.js
================================================ */


(function () {
    const toggle = document.getElementById('navToggle');
    const links = document.getElementById('navLinks');
    if (!toggle || !links) return;

    toggle.addEventListener('click', () => {
        const isOpen = links.classList.toggle('open');
        toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    });

    links.querySelectorAll('.nav-link').forEach(a => {
        a.addEventListener('click', () => {
            links.classList.remove('open');
            toggle.setAttribute('aria-expanded', 'false');
        });
    });
})();

(function () {
    const banner = document.getElementById('consentBanner');
    if (!banner) return;
    const CONSENT_KEY = 'ft-consent';

    if (!localStorage.getItem(CONSENT_KEY)) {
        banner.style.display = 'flex';
    }

    document.getElementById('consentAccept')?.addEventListener('click', () => {
        localStorage.setItem(CONSENT_KEY, 'accepted');
        banner.style.display = 'none';
    });

    document.getElementById('consentDecline')?.addEventListener('click', () => {
        localStorage.setItem(CONSENT_KEY, 'declined');
        banner.style.display = 'none';
    });
})();

function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast--${type}`;
    toast.textContent = message;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transition = 'opacity 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

function onTurnstileSuccess(token) {
    const input = document.getElementById('turnstileToken');
    if (input) input.value = token;
}

(function () {
    const form = document.getElementById('contactForm');
    if (!form) return;

    // Site anahtarı sunucu tarafında basılır (data-sitekey); istemcide değiştirmeye çalışmak
    // widget zaten kurulduktan sonra iş görmez.

    const submitBtn = document.getElementById('contactSubmitBtn');
    const submitText = document.getElementById('contactSubmitText');
    const sendingText = document.getElementById('contactSendingText');

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const name = form.Name.value.trim();
        const email = form.Email.value.trim();
        const message = form.Message.value.trim();
        const token = form.TurnstileToken.value;

        if (!name || !email || !message) {
            showToast('Lütfen tüm alanları doldurun.', 'error');
            return;
        }

        submitBtn.disabled = true;
        submitText.style.display = 'none';
        sendingText.style.display = 'inline';

        const formData = new FormData(form);

        try {
            const res = await fetch('/Home/Contact', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (res.ok) {
                showToast('Mesajınız başarıyla gönderildi!', 'success');
                form.reset();
                if (typeof turnstile !== 'undefined') {
                    turnstile.reset();
                }
            } else {
                const data = await res.json().catch(() => ({}));
                showToast(data.message || 'Mesaj gönderilemedi. Lütfen tekrar deneyin.', 'error');
            }
        } catch (err) {
            showToast('Bağlantı hatası. Lütfen tekrar deneyin.', 'error');
        } finally {
            submitBtn.disabled = false;
            submitText.style.display = 'inline';
            sendingText.style.display = 'none';
        }
    });
})();
