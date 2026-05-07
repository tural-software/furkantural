(function () {
    const form = document.getElementById('loginForm');
    if (!form) return;

    const errorBox = document.getElementById('loginErrors');
    const submitBtn = form.querySelector('.auth-submit, button[type="submit"]');
    const card = form.closest('.auth-card');

    function showErrors(errors) {
        if (!errorBox) return;
        if (!errors || errors.length === 0) {
            errorBox.innerHTML = '';
            return;
        }
        const items = errors.map((e) => `<li>${escapeHtml(e)}</li>`).join('');
        errorBox.innerHTML = `<ul>${items}</ul>`;
    }

    function escapeHtml(str) {
        return String(str).replace(/[&<>"']/g, (s) => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        })[s]);
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        showErrors([]);
        if (submitBtn) submitBtn.disabled = true;

        try {
            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) {
                const msg = `Sunucu hatası: ${response.status}`;
                showErrors([msg]);
                if (window.showToast) window.showToast('error', 'Sunucu hatası', msg);
                if (submitBtn) submitBtn.disabled = false;
                return;
            }

            const data = await response.json();

            if (!data.ok) {
                const errors = data.errors || ['Giriş başarısız.'];
                showErrors(errors);
                if (window.showToast) window.showToast('error', 'Giriş başarısız', errors.join(' '));
                if (submitBtn) submitBtn.disabled = false;
                return;
            }

            if (card) card.classList.add('is-leaving');
            await new Promise((r) => setTimeout(r, 450));
            window.location.href = data.redirect || '/Dashboard';
        } catch (err) {
            console.error(err);
            const msg = 'Beklenmeyen bir hata oluştu.';
            showErrors([msg]);
            if (window.showToast) window.showToast('error', 'Bağlantı hatası', msg);
            if (submitBtn) submitBtn.disabled = false;
        }
    });
})();
