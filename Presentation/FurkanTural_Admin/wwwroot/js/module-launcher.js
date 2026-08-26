(function () {
    const root = document.getElementById('moduleLauncher');
    if (!root) return;

    const search = document.getElementById('mlSearch');
    const empty = document.getElementById('mlEmpty');
    const panel = root.querySelector('.ml__panel');
    const rows = Array.from(root.querySelectorAll('[data-ml-row]'));
    const groups = Array.from(root.querySelectorAll('[data-ml-group]'));

    let lastFocus = null;
    let active = -1;

    const lower = (s) => (s || '').toLocaleLowerCase('tr');
    const visible = () => rows.filter((r) => !r.hidden);

    function setActive(i) {
        const list = visible();
        rows.forEach((r) => r.classList.remove('ml__row--active'));
        active = list.length === 0 ? -1 : Math.max(0, Math.min(i, list.length - 1));
        if (active < 0) return;
        const el = list[active];
        el.classList.add('ml__row--active');
        el.scrollIntoView({ block: 'nearest' });
    }

    function filter() {
        const q = lower(search.value).trim();
        rows.forEach((r) => {
            r.hidden = q !== '' && !lower(r.dataset.mlText).includes(q);
        });
        groups.forEach((g) => {
            g.hidden = !g.querySelector('[data-ml-row]:not([hidden])');
        });
        empty.hidden = visible().length > 0;
        setActive(0);
    }

    function open() {
        if (!root.hidden) return;
        lastFocus = document.activeElement;
        root.hidden = false;
        document.body.classList.add('ml-open');
        search.value = '';
        filter();
        search.focus();
    }

    function close() {
        if (root.hidden) return;
        root.hidden = true;
        document.body.classList.remove('ml-open');
        if (lastFocus && typeof lastFocus.focus === 'function') lastFocus.focus();
    }

    root.querySelectorAll('[data-ml-close]').forEach((el) => el.addEventListener('click', close));
    document.querySelectorAll('[data-ml-open]').forEach((el) => {
        el.addEventListener('click', (e) => { e.preventDefault(); open(); });
    });

    search.addEventListener('input', filter);

    search.addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown') { e.preventDefault(); setActive(active + 1); return; }
        if (e.key === 'ArrowUp') { e.preventDefault(); setActive(active - 1); return; }
        if (e.key === 'Enter') {
            const list = visible();
            if (active >= 0 && list[active]) { e.preventDefault(); list[active].click(); }
        }
    });

    rows.forEach((r) => {
        r.addEventListener('mouseenter', () => setActive(visible().indexOf(r)));
    });

    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            root.hidden ? open() : close();
            return;
        }
        if (root.hidden) return;
        if (e.key === 'Escape') { e.preventDefault(); close(); return; }
        if (e.key === 'Tab') {
            const focusables = panel.querySelectorAll('input, button, a[href]:not([hidden])');
            const list = Array.from(focusables).filter((el) => el.offsetParent !== null);
            if (list.length === 0) return;
            const first = list[0];
            const last = list[list.length - 1];
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    });
})();
