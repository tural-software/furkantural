(function () {
    const forms = document.querySelectorAll('form[data-auth-form]');
    if (!forms.length) return;

    forms.forEach((form) => {
        const submitBtn = form.querySelector('button[type="submit"]');
        const tokenInput = form.querySelector('#turnstileToken');
        const failTitle = form.getAttribute('data-fail-title') || 'İşlem başarısız';

        form.addEventListener('submit', async (e) => {
            e.preventDefault();

            if (tokenInput && !tokenInput.value) {
                if (window.showToast) {
                    window.showToast('error', failTitle,
                        'Robot doğrulaması henüz tamamlanmadı. Birkaç saniye bekleyip tekrar deneyin.');
                }
                return;
            }

            if (submitBtn) {
                submitBtn.disabled = true;
                const loadingText = submitBtn.getAttribute('data-loading-text');
                if (loadingText) {
                    submitBtn.__origText = submitBtn.textContent;
                    submitBtn.textContent = loadingText;
                }
            }

            function resetBtn() {
                if (!submitBtn) return;
                submitBtn.disabled = false;
                if (submitBtn.__origText !== undefined) {
                    submitBtn.textContent = submitBtn.__origText;
                    delete submitBtn.__origText;
                }
            }

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form),
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    const msg = `Sunucu hatası: ${response.status}`;
                    if (window.showToast) window.showToast('error', 'Sunucu hatası', msg);
                    resetBtn();
                    if (window.ftTurnstileReset) window.ftTurnstileReset();
                    return;
                }

                const data = await response.json();

                if (!data.ok) {
                    const errors = (data.errors && data.errors.length) ? data.errors : ['İşlem başarısız.'];
                    if (window.showToast) window.showToast('error', failTitle, errors.join(' '));
                    resetBtn();
                    if (window.ftTurnstileReset) window.ftTurnstileReset();
                    return;
                }

                window.location.href = data.redirect || '/Chat';
            } catch (err) {
                console.error(err);
                if (window.showToast) window.showToast('error', 'Bağlantı hatası', 'Beklenmeyen bir hata oluştu.');
                resetBtn();
            }
        });
    });
})();
