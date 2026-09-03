/**
 * DetailModal — generic config-driven record detail drawer
 *
 * Usage:
 *   DetailModal.open(config, record, onAction)
 *   DetailModal.close()
 *
 * Config shape:  { title, description, header, sections[], actions[], related }
 * Section shape: { title, icon, columns, tab: 'general'|'related'|'system', fields[] }
 * Field shape:   { label, icon, value(r), badgeVariant(r), isCode, multiline, html, requires }
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

    /* ── Denetim alanları ─────────────────────────────────────
       Her entity aynı sekiz alanı taşır; tek tanım burada durur. Sayfa yapılandırmaları
       DmAudit.section() çağırır. 'requires' sayesinde DTO'sunda olmayan alan hiç çizilmez. */
    window.DmAudit = {
        section: function () {
            return {
                tab: 'system',
                columns: 2,
                fields: [
                    {
                        label: 'Aktif', icon: 'check-circle', requires: 'isActive',
                        value: function (r) { return r.isActive ? 'Aktif' : 'Pasif'; },
                        badgeVariant: function (r) { return r.isActive ? 'success' : 'danger'; }
                    },
                    {
                        label: 'Silinmiş', icon: 'shield', requires: 'isDeleted',
                        value: function (r) { return r.isDeleted ? 'Evet' : 'Hayır'; },
                        badgeVariant: function (r) { return r.isDeleted ? 'danger' : 'neutral'; }
                    },
                    {
                        label: 'Oluşturulma', icon: 'calendar', requires: 'createdAt',
                        value: function (r) { return fmtDate(r.createdAt); }
                    },
                    {
                        label: 'Oluşturan', icon: 'user', requires: 'createdBy',
                        value: function (r) { return r.createdBy ? 'Admin (ID: ' + r.createdBy + ')' : '—'; }
                    },
                    {
                        label: 'Güncellenme', icon: 'calendar', requires: 'updatedAt',
                        value: function (r) { return fmtDate(r.updatedAt); }
                    },
                    {
                        label: 'Güncelleyen', icon: 'user', requires: 'updatedBy',
                        value: function (r) { return r.updatedBy ? 'Admin (ID: ' + r.updatedBy + ')' : '—'; }
                    },
                    {
                        label: 'Silinme', icon: 'clock', requires: 'deletedAt',
                        value: function (r) { return r.deletedAt ? fmtDate(r.deletedAt) : '—'; }
                    },
                    {
                        label: 'Silen', icon: 'user', requires: 'deletedBy',
                        value: function (r) { return r.deletedBy ? 'Admin (ID: ' + r.deletedBy + ')' : '—'; }
                    }
                ]
            };
        }
    };

    const TABS = [
        { key: 'general', label: 'Genel' },
        { key: 'related', label: 'İlişkili' },
        { key: 'system', label: 'Sistem' }
    ];

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
        return '<span class="dm-badge dm-badge--' + escHtml(variant) + '">' + escHtml(label) + '</span>';
    }

    function renderField(field, record) {
        if (!field || !field.label) {
            return '<div class="dm-field dm-field--empty"></div>';
        }
        const rawVal = field.value ? field.value(record) : '—';
        const iconHtml = field.icon
            ? '<span class="dm-field__icon">' + icon(field.icon) + '</span>'
            : '';
        let valueHtml;
        if (field.badgeVariant) {
            const v = field.badgeVariant(record);
            valueHtml = renderBadge(rawVal, v);
        } else if (field.isCode) {
            const escAttr = function (v) {
                return String(v).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            };
            const escText = function (v) {
                return String(v).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            };
            if (rawVal === '—') {
                valueHtml = '<div class="dm-code-wrap"><pre class="dm-code">—</pre></div>';
            } else {
                const words = rawVal.split(/\s+/).filter(Boolean);
                if (words.length > 20) {
                    const shortText = words.slice(0, 20).join(' ') + '…';
                    valueHtml = '<div class="dm-code-wrap">'
                        + '<pre class="dm-code" data-full="' + escAttr(rawVal) + '" data-short="' + escAttr(shortText) + '">' + escText(shortText) + '</pre>'
                        + '<button class="dm-expand-btn" data-expanded="false">Daha Fazla Göster</button>'
                        + '</div>';
                } else {
                    valueHtml = '<div class="dm-code-wrap"><pre class="dm-code">' + escText(rawVal) + '</pre></div>';
                }
            }
        } else if (field.multiline) {
            // Uzun serbest metin: 45 kelimeden sonrasi katlanir, ayni genislet dugmesiyle acilir.
            var esc = function (v) { return String(v).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); };
            var w = String(rawVal).split(/\s+/).filter(Boolean);
            if (w.length > 45) {
                var brief = w.slice(0, 45).join(' ') + '…';
                valueHtml = '<div class="dm-code-wrap">'
                    + '<p class="dm-longtext" data-full="' + esc(rawVal) + '" data-short="' + esc(brief) + '">' + esc(brief) + '</p>'
                    + '<button class="dm-expand-btn" data-expanded="false">Daha Fazla Göster</button>'
                    + '</div>';
            } else {
                valueHtml = '<p class="dm-longtext">' + esc(rawVal) + '</p>';
            }
        } else if (field.html) {
            // Trusted, config-generated HTML (e.g. <audio>, <img>) — render unescaped.
            valueHtml = '<span class="dm-field__value">' + rawVal + '</span>';
        } else {
            const safe = String(rawVal).replace(/</g, '&lt;').replace(/>/g, '&gt;');
            valueHtml = '<span class="dm-field__value">' + safe + '</span>';
        }
        return '<div class="dm-field">'
            + iconHtml
            + '<div>'
            + '<p class="dm-field__label">' + field.label + '</p>'
            + valueHtml
            + '</div>'
            + '</div>';
    }

    // DTO'da bulunmayan alan çizilmez: 'requires' anahtarı kayıtta undefined ise satır düşer.
    function visibleFields(section, record) {
        return (section.fields || []).filter(function (f) {
            return !f || !f.requires || record[f.requires] !== undefined;
        });
    }

    function renderSection(section, record) {
        const fields = visibleFields(section, record);
        if (fields.length === 0) return '';
        const titleHtml = section.title
            ? '<div class="dm-section__head">' + (section.icon ? icon(section.icon) : '') + section.title + '</div>'
            : '';
        const colClass = section.columns === 2 ? 'dm-fields--2col' : '';
        const fieldsHtml = fields.map(function (f) { return renderField(f, record); }).join('');
        return '<div class="dm-section">' + titleHtml + '<div class="dm-fields ' + colClass + '">' + fieldsHtml + '</div></div>';
    }

    function renderRecordHead(config, record) {
        const h = config.header || {};
        const iconHtml = h.icon
            ? '<span class="dm-record-head__icon">' + icon(h.icon) + '</span>'
            : '';
        const idHtml = h.idLabel
            ? '<span class="dm-record-head__id">' + h.idLabel(record) + '</span>'
            : '';
        const badgesHtml = (h.badges || []).map(function (b) {
            return renderBadge(b.label(record), b.variant(record));
        }).join('');
        return '<div class="dm-record-head">'
            + '<div class="dm-record-head__left">' + iconHtml + idHtml + '</div>'
            + '<div class="dm-record-head__badges">' + badgesHtml + '</div>'
            + '</div>';
    }

    function renderActions(actions, record) {
        return (actions || []).filter(function (a) {
            const hidden = typeof a.hidden === 'function' ? a.hidden(record) : !!a.hidden;
            return !hidden;
        }).map(function (a) {
            const iconHtml = a.icon ? icon(a.icon) : '';
            const cls = a.variant === 'primary' ? 'btn-primary' : 'btn-outline';
            const disabled = a.disabled ? ' disabled' : '';
            return '<button class="' + cls + ' dm-action-btn" data-key="' + a.key + '"' + disabled + '>' + iconHtml + a.label + '</button>';
        }).join('');
    }

    let _overlay = null;
    let _onAction = null;
    let _lastFocus = null;

    function trapFocus(e) {
        const panel = _overlay.querySelector('.dm');
        if (!panel) return;
        const list = Array.from(panel.querySelectorAll('button:not([disabled]), a[href], input, select, textarea'))
            .filter(function (el) { return el.offsetParent !== null; });
        if (list.length === 0) return;
        const first = list[0];
        const last = list[list.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    }

    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.className = 'dm-overlay dm-overlay--hidden';
        document.body.appendChild(_overlay);

        document.addEventListener('keydown', function (e) {
            if (_overlay.classList.contains('dm-overlay--hidden')) return;
            if (e.key === 'Escape') { window.DetailModal.close(); return; }
            if (e.key === 'Tab') trapFocus(e);
        });

        _overlay.addEventListener('click', function (e) {
            if (e.target === _overlay) window.DetailModal.close();
        });
    }

    function loadRelated(config, record) {
        var slot = _overlay.querySelector('[data-rel-slot]');
        if (!slot || !config.related || !record || record.id == null) return;

        var url = '/Related/Counts?entity=' + encodeURIComponent(config.related.entity)
                + '&id=' + encodeURIComponent(record.id);

        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!slot.isConnected) return;
                var items = (data && data.items) || [];
                if (items.length === 0) {
                    slot.textContent = 'Bağlı kayıt yok.';
                    return;
                }
                slot.outerHTML = items.map(function (it) {
                    return '<a class="dm-rel" href="' + escHtml(it.url || '#') + '">'
                        + '<span class="dm-rel__icon">' + icon(it.icon) + '</span>'
                        + '<span class="dm-rel__text">'
                        + '<span class="dm-rel__title">' + escHtml(it.title) + '</span>'
                        + '<span class="dm-rel__meta">' + escHtml(String(it.count)) + ' ' + escHtml(it.unit || 'kayıt') + '</span>'
                        + '</span>'
                        + '<span class="dm-rel__arrow">' + icon('chevron-right') + '</span>'
                        + '</a>';
                }).join('');
            })
            .catch(function () {
                if (slot.isConnected) slot.textContent = 'Bağlı kayıtlar alınamadı.';
            });
    }

    window.DetailModal = {
        open: function (config, record, onAction) {
            ensureOverlay();
            _onAction = onAction || null;
            _lastFocus = document.activeElement;

            const imagePreviewHtml = (config.imagePreview && typeof config.imagePreview === 'function')
                ? config.imagePreview(record)
                : '';

            const panes = TABS.map(function (t) {
                const html = (config.sections || [])
                    .filter(function (s) { return (s.tab || 'general') === t.key; })
                    .map(function (s) { return renderSection(s, record); })
                    .join('');
                var body = (t.key === 'general' ? imagePreviewHtml : '') + html;
                // İlişkili sekmesi sayıları sunucudan gelir; bölme yer tutucuyla açılır,
                // sayı gelince yerine yazılır. Sekme baştan görünür, yerinden oynamaz.
                if (t.key === 'related' && config.related && record && record.id != null) {
                    body = '<p class="dm-rel-empty" data-rel-slot>Yükleniyor…</p>' + body;
                }
                return { key: t.key, label: t.label, html: body };
            }).filter(function (p) { return p.html.trim() !== ''; });

            const active = panes.length ? panes[0].key : 'general';

            const tabsHtml = panes.length > 1
                ? '<div class="dm-tabs" role="tablist">' + panes.map(function (p) {
                    return '<button type="button" class="dm-tab' + (p.key === active ? ' dm-tab--active' : '') + '"'
                        + ' role="tab" aria-selected="' + (p.key === active) + '" data-tab="' + p.key + '">' + p.label + '</button>';
                }).join('') + '</div>'
                : '';

            const panesHtml = panes.map(function (p) {
                return '<div class="dm-pane" data-pane="' + p.key + '"' + (p.key === active ? '' : ' hidden') + '>' + p.html + '</div>';
            }).join('');

            _overlay.innerHTML =
                '<aside class="dm" role="dialog" aria-modal="true" aria-labelledby="dm-title">'
                + '<div class="dm-head">'
                + '<div>'
                + '<p class="dm-head__title" id="dm-title">' + (config.title || 'Detay') + '</p>'
                + (config.description ? '<p class="dm-head__sub">' + config.description + '</p>' : '')
                + '</div>'
                + '<button class="dm-close" aria-label="Kapat" id="dm-close-x">' + icon('close') + '</button>'
                + '</div>'
                + renderRecordHead(config, record)
                + tabsHtml
                + '<div class="dm-body">' + panesHtml + '</div>'
                + '<div class="dm-footer"><div class="dm-footer__right">' + renderActions(config.actions, record) + '</div></div>'
                + '</aside>';

            _overlay.classList.remove('dm-overlay--hidden');
            document.body.style.overflow = 'hidden';

            _overlay.querySelector('#dm-close-x').addEventListener('click', function () {
                window.DetailModal.close();
            });

            _overlay.querySelectorAll('.dm-tab').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const key = btn.dataset.tab;
                    _overlay.querySelectorAll('.dm-tab').forEach(function (b) {
                        const on = b === btn;
                        b.classList.toggle('dm-tab--active', on);
                        b.setAttribute('aria-selected', on ? 'true' : 'false');
                    });
                    _overlay.querySelectorAll('.dm-pane').forEach(function (p) {
                        p.hidden = p.dataset.pane !== key;
                    });
                    _overlay.querySelector('.dm-body').scrollTop = 0;
                });
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

            loadRelated(config, record);

            _overlay.querySelector('#dm-close-x').focus();
        },

        close: function () {
            if (!_overlay) return;
            _overlay.classList.add('dm-overlay--hidden');
            _overlay.innerHTML = '';
            document.body.style.overflow = '';
            _onAction = null;
            if (_lastFocus && typeof _lastFocus.focus === 'function') _lastFocus.focus();
            _lastFocus = null;
        }
    };
})();
