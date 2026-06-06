(function () {
    function getEls() {
        return {
            box: document.getElementById('toastBox'),
            title: document.getElementById('toastTitle'),
            msg: document.getElementById('toastMsg')
        };
    }

    window.showToast = function (type, title, msg, timeoutMs) {
        const els = getEls();
        if (!els.box) return;

        els.box.classList.toggle('error', type === 'error');
        if (els.title) els.title.textContent = title || '';
        if (els.msg) els.msg.textContent = msg || '';

        els.box.style.display = 'block';
        requestAnimationFrame(() => {
            els.box.style.opacity = '1';
        });

        clearTimeout(els.box.__hideT);
        els.box.__hideT = setTimeout(() => {
            els.box.style.opacity = '0';
            setTimeout(() => {
                els.box.style.display = 'none';
            }, 300);
        }, timeoutMs || 4000);
    };
})();
