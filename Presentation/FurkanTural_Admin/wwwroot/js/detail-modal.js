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

    /* ── İkon kayıt defteri ───────────────────────────────────
       Tek kaynak: sunucu IconLibrary.cs → _Layout.cshtml window.__ICONS.
       SVG'leri burada KOPYALAMAYIN; yeni ikon IconLibrary.cs'e eklenir. */
    const ICONS = (typeof window !== 'undefined' && window.__ICONS) ? window.__ICONS : {};

    function icon(key) {
        return ICONS[key] || '';
    }

    /* ── Date helpers ─────────────────────────────────────── */
    // Tek kanonik biçim: gelen UTC değer DAİMA Europe/Istanbul'da gösterilir (ortak FtTime).
    function fmtDate(val) {
        if (!val) return '—';
        if (window.FtTime) return FtTime.dateTime(val);
        const d = new Date(val);
        if (isNaN(d.getTime())) return '—';
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Europe/Istanbul' })
            + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul' });
    }

    // Geriye dönük uyum: dateUtc artık date ile aynıdır (UTC/yerel ikiliği kaldırıldı).
    // Expose to configs
    window.DmFmt = { date: fmtDate, dateUtc: fmtDate };

    /* ── Render helpers ───────────────────────────────────── */
    function escHtml(v) {
        return String(v)
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }
    function renderBadge(label, variant) {
        // label → element içeriği, variant → CSS class attribute. İkisini de escape et:
        // paylaşılan util; gelecekte serbest-metin label gelirse stored-XSS'i önler.
        return `<span class="dm-badge dm-badge--${escHtml(variant)}">${escHtml(label)}</span>`;
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
