(function () {
    const KEY = 'theme';
    const root = document.documentElement;

    function resolveInitial() {
        try {
            const saved = localStorage.getItem(KEY);
            if (saved === 'light' || saved === 'dark') return saved;
        } catch (_) {}
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches
            ? 'light'
            : 'dark';
    }

    function syncIcons(theme) {
        document.querySelectorAll('[data-theme-icon]').forEach((el) => {
            el.textContent = theme === 'dark' ? '🌙' : '☀️';
        });
    }

    function apply(theme, persist) {
        if (theme !== 'light' && theme !== 'dark') return;
        root.dataset.theme = theme;
        if (persist !== false) {
            try { localStorage.setItem(KEY, theme); } catch (_) {}
        }
        syncIcons(theme);
        window.dispatchEvent(new CustomEvent('theme-change', { detail: { theme } }));
    }

    if (!root.dataset.theme) apply(resolveInitial(), false);

    window.FtTheme = {
        get: () => root.dataset.theme,
        set: (t) => apply(t, true),
        toggle: () => apply(root.dataset.theme === 'light' ? 'dark' : 'light', true)
    };

    function bind() {
        document.querySelectorAll('[data-theme-toggle]').forEach((btn) => {
            if (btn.__ftBound) return;
            btn.__ftBound = true;
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                window.FtTheme.toggle();
            });
        });
        syncIcons(root.dataset.theme);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bind);
    } else {
        bind();
    }
})();
