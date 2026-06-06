(function () {
    const forms = document.querySelectorAll('form[data-auth-form]');
    if (!forms.length) return;

    forms.forEach((form) => {
        const submitBtn = form.querySelector('button[type="submit"]');
        const failTitle = form.getAttribute('data-fail-title') || 'İşlem başarısız';

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (submitBtn) submitBtn.disabled = true;

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    const msg = `Sunucu hatası: ${response.status}`;
                    if (window.showToast) window.showToast('error', 'Sunucu hatası', msg);
                    if (submitBtn) submitBtn.disabled = false;
                    if (window.turnstile) { try { window.turnstile.reset(); } catch (e) { /* yoksay */ } }
                    return;
                }

                const data = await response.json();

                if (!data.ok) {
                    const errors = (data.errors && data.errors.length) ? data.errors : ['İşlem başarısız.'];
                    if (window.showToast) window.showToast('error', failTitle, errors.join(' '));
                    if (submitBtn) submitBtn.disabled = false;
                    if (window.turnstile) { try { window.turnstile.reset(); } catch (e) { /* yoksay */ } }
                    return;
                }

                window.location.href = data.redirect || '/Chat';
            } catch (err) {
                console.error(err);
                if (window.showToast) window.showToast('error', 'Bağlantı hatası', 'Beklenmeyen bir hata oluştu.');
                if (submitBtn) submitBtn.disabled = false;
            }
        });
    });
})();
