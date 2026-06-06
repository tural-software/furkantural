/**
 * DetailModal — generic config-driven record detail modal
 *
 * Usage:
 *   DetailModal.open(config, record)
 *   DetailModal.close()
 *
 * Config shape: { title, description, header, sections[], actions[] }
 * See wwwroot/js/pages/<entity>-detail.js for concrete configs.
 */
(function () {
    'use strict';

    /* ── Inline SVG icon registry ─────────────────────────── */
    const ICONS = {
        'close': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>',
        'mail': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>',
        'check-circle': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
        'x-circle': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>',
        'calendar': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>',
        'user': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>',
        'clock': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
        'database': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>',
        'shield': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>',
        'pencil': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>',
        'subscribers': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/></svg>',
        'minus-circle': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><line x1="8" y1="12" x2="16" y2="12"/></svg>',
        'trash': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>',
        'link': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>',
        'text': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="4" y1="6" x2="20" y2="6"/><line x1="4" y1="12" x2="20" y2="12"/><line x1="4" y1="18" x2="14" y2="18"/></svg>',
        'image': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>',
        'blogs': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="8" y1="13" x2="16" y2="13"/><line x1="8" y1="17" x2="14" y2="17"/></svg>',
        'skills': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>',
        'blog-images': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="9" r="1.5"/><path d="M21 15l-5-5L5 21"/></svg>',
        'field-text': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><polyline points="4 7 4 4 20 4 20 7"/><line x1="9" y1="20" x2="15" y2="20"/><line x1="12" y1="4" x2="12" y2="20"/></svg>',
        'hash-icon': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="4" y1="9" x2="20" y2="9"/><line x1="4" y1="15" x2="20" y2="15"/><line x1="10" y1="3" x2="8" y2="21"/><line x1="16" y1="3" x2="14" y2="21"/></svg>',
        'educations': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M22 10L12 4 2 10l10 6 10-6z"/><path d="M6 12v5c3 2 9 2 12 0v-5"/></svg>',
        'experiences': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>',
        'logs': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="8" y1="12" x2="16" y2="12"/><line x1="8" y1="16" x2="16" y2="16"/><line x1="8" y1="8" x2="11" y2="8"/></svg>',
        'users': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>',
        'music': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/></svg>',
        'music-images': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="9" cy="10" r="2"/><path d="M21 15l-5-5L5 21"/></svg>',
        'projects': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>',
        'project-images': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path d="M21 15l-5-5L5 21"/><path d="M3 3l4 4"/></svg>',
        'contact': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>',
        'contact-template': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="2"/><line x1="3" y1="9" x2="21" y2="9"/><path d="M9 21V9"/></svg>',
        'file-text': '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="8" y1="11" x2="16" y2="11"/><line x1="8" y1="15" x2="16" y2="15"/><line x1="8" y1="19" x2="12" y2="19"/></svg>'
    };

    function icon(key) {
        return ICONS[key] || '';
    }

    /* ── Date helpers ─────────────────────────────────────── */
    function fmtDate(val) {
        if (!val) return '—';
        const d = new Date(val);
        if (isNaN(d.getTime())) return '—';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' })
            + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    }

    function fmtDateUtc(val) {
        if (!val) return '—';
        const d = new Date(val);
        if (isNaN(d.getTime())) return '—';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' })
            + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' });
    }

    // Expose to configs
    window.DmFmt = { date: fmtDate, dateUtc: fmtDateUtc };

    /* ── Render helpers ───────────────────────────────────── */
    function renderBadge(label, variant) {
        return `<span class="dm-badge dm-badge--${variant}">${label}</span>`;
    }

    function renderField(field, record) {
        if (!field || !field.label) {
            return '<div class="dm-field dm-field--empty"></div>';
        }
        const rawVal = field.value ? field.value(record) : '—';
        const iconHtml = field.icon
            ? `<span class="dm-field__icon">${icon(field.icon)}</span>`
            : '';
        let valueHtml;
        if (field.badgeVariant) {
            const v = field.badgeVariant(record);
            valueHtml = renderBadge(rawVal, v);
        } else if (field.isCode) {
            const escAttr = function (v) {
                return String(v).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            };
            const escHtml = function (v) {
                return String(v).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            };
            if (rawVal === '—') {
                valueHtml = '<div class="dm-code-wrap"><pre class="dm-code">—</pre></div>';
            } else {
                const words = rawVal.split(/\s+/).filter(Boolean);
                if (words.length > 20) {
                    const shortText = words.slice(0, 20).join(' ') + '\u2026';
                    valueHtml = '<div class="dm-code-wrap">'
                        + '<pre class="dm-code" data-full="' + escAttr(rawVal) + '" data-short="' + escAttr(shortText) + '">' + escHtml(shortText) + '</pre>'
                        + '<button class="dm-expand-btn" data-expanded="false">Daha Fazla Göster</button>'
                        + '</div>';
                } else {
                    valueHtml = '<div class="dm-code-wrap"><pre class="dm-code">' + escHtml(rawVal) + '</pre></div>';
                }
            }
        } else if (field.html) {
            // Trusted, config-generated HTML (e.g. <audio>, <img>) — render unescaped.
            valueHtml = `<span class="dm-field__value">${rawVal}</span>`;
        } else {
            const safe = String(rawVal).replace(/</g, '&lt;').replace(/>/g, '&gt;');
            valueHtml = `<span class="dm-field__value">${safe}</span>`;
        }
        return `<div class="dm-field">
            ${iconHtml}
            <div>
                <p class="dm-field__label">${field.label}</p>
                ${valueHtml}
            </div>
        </div>`;
    }

    function renderSection(section, record) {
        const titleHtml = section.title
            ? `<div class="dm-section__head">${section.icon ? icon(section.icon) : ''}${section.title}</div>`
            : '';
        const colClass = section.columns === 2 ? 'dm-fields--2col' : '';
        const fieldsHtml = (section.fields || []).map(f => renderField(f, record)).join('');
        return `<div class="dm-section">${titleHtml}<div class="dm-fields ${colClass}">${fieldsHtml}</div></div>`;
    }

    function renderRecordHead(config, record) {
        const h = config.header || {};
        const iconHtml = h.icon
            ? `<span class="dm-record-head__icon">${icon(h.icon)}</span>`
            : '';
        const idHtml = h.idLabel
            ? `<span class="dm-record-head__id">${h.idLabel(record)}</span>`
            : '';
        const badgesHtml = (h.badges || []).map(b =>
            renderBadge(b.label(record), b.variant(record))
        ).join('');
        return `<div class="dm-record-head">
            <div class="dm-record-head__left">${iconHtml}${idHtml}</div>
            <div class="dm-record-head__badges">${badgesHtml}</div>
        </div>`;
    }

    function renderActions(actions, record) {
        return (actions || []).filter(a => {
            const hidden = typeof a.hidden === 'function' ? a.hidden(record) : !!a.hidden;
            return !hidden;
        }).map(a => {
            const iconHtml = a.icon ? icon(a.icon) : '';
            const cls = a.variant === 'primary' ? 'btn-primary' : 'btn-outline';
            const disabled = a.disabled ? ' disabled' : '';
            return `<button class="${cls} dm-action-btn" data-key="${a.key}"${disabled}>${iconHtml}${a.label}</button>`;
        }).join('');
    }

    /* ── Modal lifecycle ──────────────────────────────────── */
    let _overlay = null;
    let _onAction = null;

    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.className = 'dm-overlay dm-overlay--hidden';
        document.body.appendChild(_overlay);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') window.DetailModal.close();
        });

        _overlay.addEventListener('click', function (e) {
            if (e.target === _overlay) window.DetailModal.close();
        });
    }

    window.DetailModal = {
        open: function (config, record, onAction) {
            ensureOverlay();
            _onAction = onAction || null;

            const sectionsHtml = (config.sections || [])
                .map(s => renderSection(s, record)).join('');
            const actionsHtml = renderActions(config.actions, record);

            const imagePreviewHtml = (config.imagePreview && typeof config.imagePreview === 'function')
                ? config.imagePreview(record)
                : '';

            _overlay.innerHTML = `
                <div class="dm" role="dialog" aria-modal="true" aria-labelledby="dm-title">
                    <div class="dm-head">
                        <div>
                            <p class="dm-head__title" id="dm-title">${config.title || 'Detay'}</p>
                            ${config.description ? `<p class="dm-head__sub">${config.description}</p>` : ''}
                        </div>
                        <button class="dm-close" aria-label="Kapat" id="dm-close-x">${icon('close')}</button>
                    </div>
                    ${renderRecordHead(config, record)}
                    ${imagePreviewHtml}
                    <div class="dm-body">${sectionsHtml}</div>
                    <div class="dm-footer">
                        <div class="dm-footer__right">${actionsHtml}</div>
                    </div>
                </div>`;

            _overlay.classList.remove('dm-overlay--hidden');
            document.body.style.overflow = 'hidden';

            _overlay.querySelector('#dm-close-x').addEventListener('click', function () {
                window.DetailModal.close();
            });

            _overlay.querySelectorAll('.dm-action-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const key = btn.dataset.key;
                    if (key === 'close') { window.DetailModal.close(); return; }
                    if (_onAction) _onAction(key, record);
                });
            });

            _overlay.querySelectorAll('.dm-expand-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var pre = btn.previousElementSibling;
                    var expanded = btn.dataset.expanded === 'true';
                    if (expanded) {
                        pre.textContent = pre.dataset.short;
                        btn.textContent = 'Daha Fazla Göster';
                        btn.dataset.expanded = 'false';
                    } else {
                        pre.textContent = pre.dataset.full;
                        btn.textContent = 'Daha Az Göster';
                        btn.dataset.expanded = 'true';
                    }
                });
            });
        },

        close: function () {
            if (!_overlay) return;
            _overlay.classList.add('dm-overlay--hidden');
            _overlay.innerHTML = '';
            document.body.style.overflow = '';
            _onAction = null;
        }
    };
})();
