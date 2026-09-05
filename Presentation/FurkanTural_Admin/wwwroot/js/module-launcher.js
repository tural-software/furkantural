(function () {
    const root = document.getElementById('moduleLauncher');
    if (!root) return;

    const search = document.getElementById('mlSearch');
    const empty = document.getElementById('mlEmpty');
    const records = document.getElementById('mlRecords');
    const panel = root.querySelector('.ml__panel');
    const staticRows = Array.from(root.querySelectorAll('[data-ml-row]'));
    const groups = Array.from(root.querySelectorAll('[data-ml-group]'));
    const searchUrl = root.dataset.mlSearchUrl || '/Search';

    let lastFocus = null;
    let active = -1;
    let dynamicRows = [];
    let timer = null;
    let inflight = null;
    let requestNo = 0;

    const lower = (s) => (s || '').toLocaleLowerCase('tr');
    const allRows = () => staticRows.concat(dynamicRows);
    const visible = () => allRows().filter((r) => !r.hidden);

    function setActive(i) {
        const list = visible();
        allRows().forEach((r) => r.classList.remove('ml__row--active'));
        active = list.length === 0 ? -1 : Math.max(0, Math.min(i, list.length - 1));
        if (active < 0) return;
        const el = list[active];
        el.classList.add('ml__row--active');
        el.scrollIntoView({ block: 'nearest' });
    }

    function updateEmpty() {
        empty.hidden = visible().length > 0;
    }

    function clearRecords() {
        dynamicRows = [];
        if (!records) return;
        records.innerHTML = '';
        records.hidden = true;
    }

    function iconFor(slug) {
        const row = staticRows.find((r) => r.dataset.mlSlug === slug);
        const icon = row ? row.querySelector('.ml__row-icon') : null;
        return icon ? icon.cloneNode(true) : null;
    }

    function renderRecords(data) {
        clearRecords();
        if (!records) return;
        const found = (data && data.groups) || [];
        const total = found.reduce((n, g) => n + (g.items || []).length, 0);
        if (total === 0) { updateEmpty(); return; }

        const section = document.createElement('section');
        section.className = 'ml__group';
        const title = document.createElement('h3');
        title.className = 'ml__group-title';
        title.textContent = 'Kayıtlar ';
        const count = document.createElement('span');
        count.className = 'ml__group-count';
        count.textContent = String(total);
        title.appendChild(count);
        section.appendChild(title);

        found.forEach((g) => {
            (g.items || []).forEach((item) => {
                const a = document.createElement('a');
                a.className = 'ml__row';
                a.href = item.url || '#';
                a.setAttribute('data-ml-row', '');
                const icon = iconFor(g.slug);
                if (icon) a.appendChild(icon);
                const text = document.createElement('span');
                text.className = 'ml__row-text';
                const label = document.createElement('span');
                label.className = 'ml__row-title';
                label.textContent = item.label || ('#' + item.id);
                const unit = document.createElement('span');
                unit.className = 'ml__row-unit';
                unit.textContent = g.title || g.slug;
                text.appendChild(label);
                text.appendChild(unit);
                a.appendChild(text);
                a.addEventListener('mouseenter', () => setActive(visible().indexOf(a)));
                section.appendChild(a);
                dynamicRows.push(a);
            });
        });

        records.appendChild(section);
        records.hidden = false;
        updateEmpty();
        if (active < 0) setActive(0);
    }

    function fetchRecords(q) {
        const no = ++requestNo;
        if (inflight) inflight.abort();
        inflight = new AbortController();
        fetch(searchUrl + '?q=' + encodeURIComponent(q), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            signal: inflight.signal
        })
            .then((r) => (r.ok ? r.json() : null))
            .then((data) => { if (no === requestNo) renderRecords(data); })
            .catch(() => { if (no === requestNo) { clearRecords(); updateEmpty(); } });
    }

    function filter() {
        const q = lower(search.value).trim();
        staticRows.forEach((r) => {
            r.hidden = q !== '' && !lower(r.dataset.mlText).includes(q);
        });
        groups.forEach((g) => {
            g.hidden = !g.querySelector('[data-ml-row]:not([hidden])');
        });
        if (timer) clearTimeout(timer);
        if (q.length < 2) {
            requestNo++;
            if (inflight) inflight.abort();
            clearRecords();
        } else {
            timer = setTimeout(() => fetchRecords(search.value.trim()), 250);
        }
        updateEmpty();
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

    staticRows.forEach((r) => {
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
