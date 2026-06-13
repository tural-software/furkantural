/**
 * HtmlPreviewModal — sandboxed HTML template preview
 *
 * API:
 *   HtmlPreviewModal.open(templateName, htmlContent)
 *   HtmlPreviewModal.close()
 */
(function () {
    'use strict';

    let _overlay = null;

    /* ── İkonlar tek kaynaktan (window.__ICONS / IconLibrary.cs) ── */
    const _ic = (window.__ICONS || {});
    const EYE_ICON = _ic['eye'] || '';
    const COPY_ICON = _ic['copy'] || '';
    const CHECK_ICON = _ic['check'] || '';
    const CLOSE_ICON = _ic['close'] || '';

    /* ── HTML escape helper ───────────────────────────────── */
    function escapeHtml(str) {
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /* ── Overlay setup ───────────────────────────────────── */
    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.className = 'hpm-overlay hpm-overlay--hidden';
        document.body.appendChild(_overlay);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') window.HtmlPreviewModal.close();
        });

        _overlay.addEventListener('click', function (e) {
            if (e.target === _overlay) window.HtmlPreviewModal.close();
        });
    }

    /* ── Build modal HTML ─────────────────────────────────── */
    function buildModal(name, htmlContent) {
        const srcDoc = escapeHtml(htmlContent);
        const srcRaw = escapeHtml(htmlContent);

        return `
<div class="hpm" role="dialog" aria-modal="true" aria-labelledby="hpm-title">
    <div class="hpm-head">
        <div class="hpm-head__left">
            <span class="hpm-head__icon">${EYE_ICON}</span>
            <div>
                <p class="hpm-head__title" id="hpm-title">Şablon Önizlemesi</p>
                <p class="hpm-head__sub">${escapeHtml(name)}</p>
            </div>
        </div>
        <button class="hpm-close" id="hpm-close-x" aria-label="Kapat">${CLOSE_ICON}</button>
    </div>

    <div class="hpm-tabs" role="tablist">
        <button class="hpm-tab hpm-tab--active" data-tab="preview" role="tab" aria-selected="true">
            Önizleme
        </button>
        <button class="hpm-tab" data-tab="source" role="tab" aria-selected="false">
            HTML Kaynağı
        </button>
    </div>

    <div class="hpm-body">
        <div class="hpm-panel" id="hpm-preview-panel">
            <iframe
                class="hpm-iframe"
                sandbox=""
                title="Şablon önizlemesi"
                srcdoc="${srcDoc}">
            </iframe>
        </div>
        <div class="hpm-panel hpm-panel--hidden" id="hpm-source-panel">
            <div class="hpm-source-toolbar">
                <button class="hpm-copy-btn" id="hpm-copy-btn" aria-label="HTML kaynağını kopyala">
                    <span class="hpm-copy-btn__icon">${COPY_ICON}</span>
                    <span class="hpm-copy-btn__label">Kopyala</span>
                </button>
            </div>
            <pre class="hpm-pre"><code>${srcRaw}</code></pre>
        </div>
    </div>

    <div class="hpm-footer">
        <button class="btn-outline hpm-footer-close" id="hpm-footer-close">Kapat</button>
    </div>
</div>`;
    }

    /* ── Bind events ─────────────────────────────────────── */
    function bindEvents(htmlContent) {
        /* Close buttons */
        _overlay.querySelector('#hpm-close-x').addEventListener('click', function () {
            window.HtmlPreviewModal.close();
        });
        _overlay.querySelector('#hpm-footer-close').addEventListener('click', function () {
            window.HtmlPreviewModal.close();
        });

        /* Tabs */
        const tabs = _overlay.querySelectorAll('.hpm-tab');
        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                tabs.forEach(function (t) {
                    t.classList.remove('hpm-tab--active');
                    t.setAttribute('aria-selected', 'false');
                });
                tab.classList.add('hpm-tab--active');
                tab.setAttribute('aria-selected', 'true');

                const isPreview = tab.dataset.tab === 'preview';
                _overlay.querySelector('#hpm-preview-panel').classList.toggle('hpm-panel--hidden', !isPreview);
                _overlay.querySelector('#hpm-source-panel').classList.toggle('hpm-panel--hidden', isPreview);
            });
        });

        /* Copy button */
        const copyBtn = _overlay.querySelector('#hpm-copy-btn');
        const copyIcon = copyBtn.querySelector('.hpm-copy-btn__icon');
        const copyLabel = copyBtn.querySelector('.hpm-copy-btn__label');

        copyBtn.addEventListener('click', function () {
            if (!navigator.clipboard) {
                /* Fallback for older browsers */
                const ta = document.createElement('textarea');
                ta.value = htmlContent;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.select();
                document.execCommand('copy');
                document.body.removeChild(ta);
                showCopied(copyIcon, copyLabel);
                return;
            }
            navigator.clipboard.writeText(htmlContent).then(function () {
                showCopied(copyIcon, copyLabel);
            });
        });
    }

    function showCopied(iconEl, labelEl) {
        iconEl.innerHTML = CHECK_ICON;
        labelEl.textContent = 'Kopyalandı!';
        setTimeout(function () {
            iconEl.innerHTML = COPY_ICON;
            labelEl.textContent = 'Kopyala';
        }, 2200);
    }

    /* ── Public API ──────────────────────────────────────── */
    window.HtmlPreviewModal = {
        open: function (templateName, htmlContent) {
            ensureOverlay();
            _overlay.innerHTML = buildModal(templateName || 'Şablon', htmlContent || '');
            _overlay.classList.remove('hpm-overlay--hidden');
            document.body.style.overflow = 'hidden';
            bindEvents(htmlContent || '');
        },

        close: function () {
            if (!_overlay) return;
            _overlay.classList.add('hpm-overlay--hidden');
            _overlay.innerHTML = '';
            document.body.style.overflow = '';
        }
    };
})();
