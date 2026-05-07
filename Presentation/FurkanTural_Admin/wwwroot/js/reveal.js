(function () {
    function init() {
        const reveals = document.querySelectorAll('.reveal');
        if (!reveals.length || !('IntersectionObserver' in window)) {
            reveals.forEach((el) => el.classList.add('active'));
            return;
        }

        const obs = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('active');
                    obs.unobserve(entry.target);
                }
            });
        }, { threshold: 0.15 });

        reveals.forEach((el) => obs.observe(el));
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
