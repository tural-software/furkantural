/**
 * FormModal — generic config-driven form modal
 *
 * Usage:
 *   FormModal.open(config, initialValues)
 *   FormModal.close()
 *
 * Config:
 *   {
 *     title:       string,
 *     description: string,
 *     submitUrl:   string,
 *     submitLabel: string,   // 'Ekle' | 'Güncelle'
 *     fields: [
 *       { name, label, type, required, maxLength, min, max, placeholder, helpText }
 *     ],
 *     onSuccess: function()
 *   }
 *
 * Field types: 'text' | 'number' | 'checkbox'
 *
 * Depends on: form-modal.css (dm-close, dm-fade-in, dm-slide-up from detail-modal.css)
 */
(function () {
    'use strict';

    // İkon tek kaynaktan (window.__ICONS / IconLibrary.cs) okunur.
    var CLOSE_ICON = (window.__ICONS || {})['close'] || '';

    var _overlay = null;
    var _config  = null;

    function getToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function proficiencyLevel(val) {
        if (val >= 71) return 'high';
        if (val >= 41) return 'mid';
        return 'low';
    }

    function ensureOverlay() {
        if (_overlay) return;
        _overlay = document.createElement('div');
        _overlay.className = 'fm-overlay fm-overlay--hidden';
        document.body.appendChild(_overlay);

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') FormModal.close();
        });

        _overlay.addEventListener('click', function (e) {
            if (e.target === _overlay) FormModal.close();
        });
    }

    function buildTextField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var val = (value !== undefined && value !== null) ? String(value) : '';
        var maxLen = field.maxLength ? ' maxlength="' + field.maxLength + '"' : '';
        var ph = field.placeholder ? ' placeholder="' + escAttr(field.placeholder) + '"' : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<input type="text" class="fm-field__input" name="' + field.name + '" value="' + escAttr(val) + '"' + maxLen + ph + ' autocomplete="off" />' +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildPasswordField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var maxLen = field.maxLength ? ' maxlength="' + field.maxLength + '"' : '';
        var ph = field.placeholder ? ' placeholder="' + escAttr(field.placeholder) + '"' : '';
        var inputId = 'fmpw-' + field.name;
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '" for="' + inputId + '">' + escHtml(field.label) + '</label>' +
            '<div class="fm-field__pw">' +
            '<input type="password" id="' + inputId + '" class="fm-field__input" name="' + field.name + '"' + maxLen + ph + ' autocomplete="new-password" />' +
            '<button type="button" class="fm-pw-gen" data-password-generate="#' + inputId + '" data-password-hint="#fmpwnote-' + field.name + '">Üret</button>' +
            '</div>' +
            '<p class="fm-field__hint">En az 6 karakter; bir büyük harf, bir küçük harf, bir rakam ve bir sembol (' +
            '<span class="fm-field__symbols">!#$%()*+,-./:;=?@[]^_{|}~</span>).</p>' +
            '<p class="fm-field__note" id="fmpwnote-' + field.name + '" role="status" aria-live="polite"></p>' +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildNumberField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var val = (value !== undefined && value !== null) ? String(value) : '';
        var minAttr = (field.min !== undefined) ? ' min="' + field.min + '"' : '';
        var maxAttr = (field.max !== undefined) ? ' max="' + field.max + '"' : '';
        var ph = field.placeholder ? ' placeholder="' + escAttr(field.placeholder) + '"' : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<input type="number" class="fm-field__input" name="' + field.name + '" value="' + escAttr(val) + '"' + minAttr + maxAttr + ph + ' autocomplete="off" />' +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildCheckboxField(field, value) {
        var checked = (value === true || value === 'true' || value === 1);
        var onClass = checked ? ' fm-toggle--on' : '';
        var labelText = checked ? 'Aktif' : 'Pasif';
        var labelClass = checked ? ' fm-toggle__label--on' : ' fm-toggle__label--off';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label">' + escHtml(field.label) + '</label>' +
            '<div class="fm-toggle' + onClass + '" id="fmtoggle-' + field.name + '" role="switch" aria-checked="' + checked + '" tabindex="0">' +
            '<div class="fm-toggle__track"><div class="fm-toggle__thumb"></div></div>' +
            '<span class="fm-toggle__label' + labelClass + '" id="fmtogglelbl-' + field.name + '">' + labelText + '</span>' +
            '<input type="hidden" name="' + field.name + '" id="fmtoggleval-' + field.name + '" value="' + (checked ? 'true' : 'false') + '" />' +
            '</div>' +
            '</div>';
    }

    // Çoklu seçim — onay kutusu listesi (örn. blog kategorileri). value = seçili değerler dizisi.
    function buildMultiselectField(field, value) {
        var selected = Array.isArray(value) ? value.map(String) : [];
        var options = field.options || [];
        var boxes = options.map(function (o) {
            var ov = String(o.value);
            var checked = selected.indexOf(ov) !== -1 ? ' checked' : '';
            var dot = o.color ? '<span class="fm-ms__dot" style="background:' + escAttr(o.color) + '"></span>' : '';
            return '<label class="fm-ms__opt">' +
                '<input type="checkbox" name="' + field.name + '" value="' + escAttr(ov) + '"' + checked + ' />' +
                dot + '<span>' + escHtml(o.label || '') + '</span>' +
                '</label>';
        }).join('');
        var helpHtml = field.helpText ? '<p class="fm-field__help">' + escHtml(field.helpText) + '</p>' : '';
        var emptyHtml = options.length === 0 ? '<p class="fm-field__help">Seçilebilir kayıt yok.</p>' : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label">' + escHtml(field.label) + '</label>' +
            '<div class="fm-ms">' + boxes + emptyHtml + '</div>' +
            helpHtml +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildFileField(field) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var accept = field.accept ? ' accept="' + escAttr(field.accept) + '"' : '';
        var helpHtml = field.helpText
            ? '<p class="fm-field__help">' + escHtml(field.helpText) + '</p>'
            : '';
        var previewHtml = field.accept && field.accept.indexOf('image') !== -1
            ? '<div class="fm-field__preview" id="fmpreview-' + field.name + '"><img src="" alt="Önizleme" /></div>'
            : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<input type="file" class="fm-field__file" name="' + field.name + '"' + accept + ' />' +
            helpHtml +
            previewHtml +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildHiddenField(field, value) {
        var val = (value !== undefined && value !== null) ? String(value) : '';
        return '<input type="hidden" name="' + field.name + '" value="' + escAttr(val) + '" />';
    }

    function buildSearchableSelectField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var options  = field.options || [];
        var ph       = field.placeholder || 'Seçin veya arayın...';
        var hiddenVal = (value !== undefined && value !== null) ? String(value) : '';
        var displayVal = '';
        if (hiddenVal) {
            for (var i = 0; i < options.length; i++) {
                if (String(options[i].value) === hiddenVal) { displayVal = options[i].label || ''; break; }
            }
        }
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<div class="fm-ss" id="fmss-' + field.name + '">' +
            '<input type="text" class="fm-ss__input fm-field__input" placeholder="' + escAttr(ph) + '" value="' + escAttr(displayVal) + '" autocomplete="off" />' +
            '<div class="fm-ss__dropdown" role="listbox"></div>' +
            '</div>' +
            '<input type="hidden" name="' + field.name + '" value="' + escAttr(hiddenVal) + '" />' +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildDateField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var val = (value !== undefined && value !== null) ? String(value) : '';
        var minAttr = field.min ? ' min="' + escAttr(field.min) + '"' : '';
        var maxAttr = field.max ? ' max="' + escAttr(field.max) + '"' : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<input type="date" class="fm-field__input" name="' + field.name + '" value="' + escAttr(val) + '"' + minAttr + maxAttr + ' autocomplete="off" />' +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildTextareaField(field, value) {
        var reqClass = field.required ? ' fm-field__label--required' : '';
        var val = (value !== undefined && value !== null) ? String(value) : '';
        var maxLen = field.maxLength ? ' maxlength="' + field.maxLength + '"' : '';
        var ph = field.placeholder ? ' placeholder="' + escAttr(field.placeholder) + '"' : '';
        var rows = field.rows ? ' rows="' + field.rows + '"' : ' rows="5"';
        var helpHtml = field.helpText
            ? '<p class="fm-field__help">' + escHtml(field.helpText) + '</p>'
            : '';
        return '<div class="fm-field" data-field="' + field.name + '">' +
            '<label class="fm-field__label' + reqClass + '">' + escHtml(field.label) + '</label>' +
            '<textarea class="fm-field__textarea" name="' + field.name + '"' + maxLen + ph + rows + '>' + escHtml(val) + '</textarea>' +
            helpHtml +
            '<p class="fm-field__error" id="fmerr-' + field.name + '"></p>' +
            '</div>';
    }

    function buildField(field, initialValues) {
        var value = initialValues ? initialValues[field.name] : undefined;
        if (field.type === 'number')            return buildNumberField(field, value);
        if (field.type === 'checkbox')          return buildCheckboxField(field, value);
        if (field.type === 'textarea')          return buildTextareaField(field, value);
        if (field.type === 'file')              return buildFileField(field);
        if (field.type === 'hidden')            return buildHiddenField(field, value);
        if (field.type === 'searchable-select') return buildSearchableSelectField(field, value);
        if (field.type === 'multiselect')       return buildMultiselectField(field, value);
        if (field.type === 'date')              return buildDateField(field, value);
        if (field.type === 'password')          return buildPasswordField(field, value);
        return buildTextField(field, value);
    }

    function escHtml(s) {
        return String(s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }
    function escAttr(s) {
        return String(s || '').replace(/&/g,'&amp;').replace(/"/g,'&quot;').replace(/</g,'&lt;');
    }

    function validateField(field, el) {
        var errEl = _overlay.querySelector('#fmerr-' + field.name);
        if (!errEl) return true;

        var value = el.value;
        var err = '';

        if (field.type === 'checkbox') return true;

        if (field.required && (!value || value.trim() === '')) {
            err = field.label + ' zorunludur.';
        } else if (field.maxLength && value.length > field.maxLength) {
            err = 'En fazla ' + field.maxLength + ' karakter girilebilir.';
        } else if (field.type === 'number' && value !== '') {
            var num = Number(value);
            if (isNaN(num)) {
                err = 'Geçerli bir sayı girin.';
            } else if (field.min !== undefined && num < field.min) {
                err = 'En az ' + field.min + ' olmalıdır.';
            } else if (field.max !== undefined && num > field.max) {
                err = 'En fazla ' + field.max + ' olabilir.';
            }
        }

        if (err) {
            el.classList.add('fm-field__input--error');
            el.classList.add('fm-field__textarea--error');
            errEl.textContent = err;
            errEl.classList.add('fm-field__error--visible');
            return false;
        } else {
            el.classList.remove('fm-field__input--error');
            el.classList.remove('fm-field__textarea--error');
            errEl.textContent = '';
            errEl.classList.remove('fm-field__error--visible');
            return true;
        }
    }

    function validateFileField(field) {
        var errEl = _overlay.querySelector('#fmerr-' + field.name);
        var fileInput = _overlay.querySelector('[name="' + field.name + '"]');
        if (field.required && (!fileInput || !fileInput.files || !fileInput.files[0])) {
            if (errEl) { errEl.textContent = field.label + ' zorunludur.'; errEl.classList.add('fm-field__error--visible'); }
            return false;
        }
        if (fileInput && fileInput.files && fileInput.files[0] && field.maxSizeBytes) {
            if (fileInput.files[0].size > field.maxSizeBytes) {
                var mb = (field.maxSizeBytes / 1024 / 1024).toFixed(0);
                if (errEl) { errEl.textContent = 'Dosya boyutu en fazla ' + mb + ' MB olabilir.'; errEl.classList.add('fm-field__error--visible'); }
                return false;
            }
        }
        if (errEl) { errEl.textContent = ''; errEl.classList.remove('fm-field__error--visible'); }
        return true;
    }

    function validateSearchableSelectField(field) {
        var hiddenEl  = _overlay.querySelector('[name="' + field.name + '"]');
        var wrapper   = _overlay.querySelector('#fmss-' + field.name);
        var textInput = wrapper ? wrapper.querySelector('.fm-ss__input') : null;
        var errEl     = _overlay.querySelector('#fmerr-' + field.name);
        if (field.required && (!hiddenEl || !hiddenEl.value)) {
            if (textInput) textInput.classList.add('fm-field__input--error');
            if (errEl) { errEl.textContent = field.label + ' seçimi zorunludur.'; errEl.classList.add('fm-field__error--visible'); }
            return false;
        }
        if (textInput) textInput.classList.remove('fm-field__input--error');
        if (errEl) { errEl.textContent = ''; errEl.classList.remove('fm-field__error--visible'); }
        return true;
    }

    function validateAll(fields) {
        var valid = true;
        fields.forEach(function (field) {
            if (field.type === 'checkbox' || field.type === 'hidden' || field.type === 'multiselect') return;
            if (field.type === 'file') { if (!validateFileField(field)) valid = false; return; }
            if (field.type === 'searchable-select') { if (!validateSearchableSelectField(field)) valid = false; return; }
            var el = _overlay.querySelector('[name="' + field.name + '"]');
            if (el && !validateField(field, el)) valid = false;
        });
        return valid;
    }

    function bindToggles(fields) {
        fields.forEach(function (field) {
            if (field.type !== 'checkbox') return;
            var toggle = _overlay.querySelector('#fmtoggle-' + field.name);
            var hidden = _overlay.querySelector('#fmtoggleval-' + field.name);
            var label  = _overlay.querySelector('#fmtogglelbl-' + field.name);
            if (!toggle) return;

            function flip() {
                var isOn = toggle.classList.contains('fm-toggle--on');
                if (isOn) {
                    toggle.classList.remove('fm-toggle--on');
                    toggle.setAttribute('aria-checked', 'false');
                    hidden.value = 'false';
                    label.textContent = 'Pasif';
                    label.className = 'fm-toggle__label fm-toggle__label--off';
                } else {
                    toggle.classList.add('fm-toggle--on');
                    toggle.setAttribute('aria-checked', 'true');
                    hidden.value = 'true';
                    label.textContent = 'Aktif';
                    label.className = 'fm-toggle__label fm-toggle__label--on';
                }
            }

            toggle.addEventListener('click', flip);
            toggle.addEventListener('keydown', function (e) {
                if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); flip(); }
            });
        });
    }

    /* ── Searchable-select helpers ───────────────────────── */

    function buildDropdownItems(options, query) {
        var lower    = (query || '').toLowerCase().trim();
        var filtered = lower
            ? options.filter(function (o) { return (o.label || '').toLowerCase().indexOf(lower) !== -1; })
            : options;
        if (filtered.length === 0)
            return '<div class="fm-ss__empty">Sonuç bulunamadı.</div>';
        return filtered.map(function (o) {
            return '<div class="fm-ss__option" data-value="' + escAttr(String(o.value)) + '" role="option">' + escHtml(o.label || '') + '</div>';
        }).join('');
    }

    function bindSearchableSelects(fields) {
        fields.forEach(function (field) {
            if (field.type !== 'searchable-select') return;
            var options     = field.options || [];
            var wrapper     = _overlay.querySelector('#fmss-' + field.name);
            if (!wrapper) return;
            var textInput   = wrapper.querySelector('.fm-ss__input');
            var dropdown    = wrapper.querySelector('.fm-ss__dropdown');
            var hiddenInput = _overlay.querySelector('[name="' + field.name + '"]');

            function populateDropdown(query) {
                dropdown.innerHTML = buildDropdownItems(options, query);
                dropdown.classList.add('fm-ss__dropdown--open');
                dropdown.querySelectorAll('.fm-ss__option').forEach(function (opt) {
                    opt.addEventListener('mousedown', function (e) {
                        e.preventDefault();
                        hiddenInput.value = opt.dataset.value;
                        textInput.value   = opt.textContent.trim();
                        dropdown.classList.remove('fm-ss__dropdown--open');
                        dropdown.innerHTML = '';
                        var errEl = _overlay.querySelector('#fmerr-' + field.name);
                        if (errEl) { errEl.textContent = ''; errEl.classList.remove('fm-field__error--visible'); }
                        textInput.classList.remove('fm-field__input--error');
                    });
                });
            }

            textInput.addEventListener('focus', function () { populateDropdown(textInput.value); });
            textInput.addEventListener('input', function () {
                hiddenInput.value = '';
                populateDropdown(textInput.value);
            });
            textInput.addEventListener('blur', function () {
                setTimeout(function () {
                    dropdown.classList.remove('fm-ss__dropdown--open');
                    dropdown.innerHTML = '';
                    if (textInput.value && !hiddenInput.value) { textInput.value = ''; }
                }, 160);
            });
        });
    }

    function buildModal(config, initialValues) {
        var fieldsHtml = config.fields.map(function (f) { return buildField(f, initialValues); }).join('');
        var note = config.footerNote === undefined
            ? 'Kaydedilen değişiklikler sistem kayıtlarına yazılır.'
            : config.footerNote;
        // İçerik-yoğun formlar için geniş varyant (config.size === 'large').
        var sizeClass = config.size === 'large' ? ' fm--large' : '';

        return '<div class="fm' + sizeClass + '" role="dialog" aria-modal="true" aria-labelledby="fm-title">' +
            '<div class="fm-head">' +
            '<div>' +
            '<p class="fm-head__title" id="fm-title">' + escHtml(config.title) + '</p>' +
            '<p class="fm-head__sub">' + escHtml(config.description || '') + '</p>' +
            '</div>' +
            '<button class="dm-close" id="fm-close-x" aria-label="Kapat">' + CLOSE_ICON + '</button>' +
            '</div>' +
            '<div class="fm-body">' + fieldsHtml + '</div>' +
            '<p class="fm-error-banner" id="fm-error-banner"></p>' +
            '<div class="fm-footer">' +
            (note ? '<p class="fm-footer__note">' + escHtml(note) + '</p>' : '') +
            '<button class="btn-outline" id="fm-cancel">İptal</button>' +
            '<button class="btn-primary" id="fm-submit">' + escHtml(config.submitLabel || 'Kaydet') + '</button>' +
            '</div>' +
            '</div>';
    }

    function doSubmit(config, fields) {
        var banner = _overlay.querySelector('#fm-error-banner');
        var submitBtn = _overlay.querySelector('#fm-submit');
        var fm = _overlay.querySelector('.fm');

        if (!validateAll(fields)) return;

        var data = new FormData();
        data.append('__RequestVerificationToken', getToken());

        fields.forEach(function (field) {
            if (field.type === 'file') {
                var fileInput = _overlay.querySelector('[name="' + field.name + '"]');
                if (fileInput && fileInput.files && fileInput.files[0]) {
                    data.append(field.name, fileInput.files[0], fileInput.files[0].name);
                }
            } else if (field.type === 'multiselect') {
                // Seçili tüm onay kutularını aynı ad altında ekle → List<T> model binding.
                _overlay.querySelectorAll('input[type="checkbox"][name="' + field.name + '"]').forEach(function (b) {
                    if (b.checked) data.append(field.name, b.value);
                });
            } else {
                var el = _overlay.querySelector('[name="' + field.name + '"]');
                if (el) data.append(field.name, el.value);
            }
        });

        fm.classList.add('fm--loading');
        submitBtn.textContent = 'Kaydediliyor...';
        submitBtn.disabled = true;
        if (banner) { banner.textContent = ''; banner.classList.remove('fm-error-banner--visible'); }

        fetch(config.submitUrl, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            body: data
        })
        .then(function (r) {
            if (r.status === 401) { window.location.href = '/Auth/Login'; return; }

            if (r.ok) {
                FormModal.close();
                if (typeof config.onSuccess === 'function') config.onSuccess();
            } else {
                return r.text().then(function (body) {
                    var msg = '';
                    try { msg = JSON.parse(body).message || ''; } catch (_) {}
                    var errorText = msg || 'İşlem gerçekleştirilemedi.';
                    if (banner) {
                        banner.textContent = errorText;
                        banner.classList.add('fm-error-banner--visible');
                    }
                    fm.classList.remove('fm--loading');
                    submitBtn.textContent = config.submitLabel || 'Kaydet';
                    submitBtn.disabled = false;
                });
            }
        })
        .catch(function () {
            if (banner) {
                banner.textContent = 'Sunucuya bağlanılamadı.';
                banner.classList.add('fm-error-banner--visible');
            }
            fm.classList.remove('fm--loading');
            submitBtn.textContent = config.submitLabel || 'Kaydet';
            submitBtn.disabled = false;
        });
    }

    window.FormModal = {
        open: function (config, initialValues) {
            ensureOverlay();
            _config = config;

            _overlay.innerHTML = buildModal(config, initialValues || {});
            _overlay.classList.remove('fm-overlay--hidden');
            document.body.style.overflow = 'hidden';

            var fields = config.fields || [];
            bindToggles(fields);
            bindSearchableSelects(fields);

            fields.forEach(function (field) {
                if (field.type !== 'file') return;
                if (!field.accept || field.accept.indexOf('image') === -1) return;
                var fileInput = _overlay.querySelector('[name="' + field.name + '"]');
                var previewBox = _overlay.querySelector('#fmpreview-' + field.name);
                if (!fileInput || !previewBox) return;
                fileInput.addEventListener('change', function () {
                    var file = fileInput.files && fileInput.files[0];
                    if (file && file.type.indexOf('image') === 0) {
                        var reader = new FileReader();
                        reader.onload = function (e) {
                            previewBox.querySelector('img').src = e.target.result;
                            previewBox.classList.add('fm-field__preview--visible');
                        };
                        reader.readAsDataURL(file);
                    } else {
                        previewBox.querySelector('img').src = '';
                        previewBox.classList.remove('fm-field__preview--visible');
                    }
                });
            });

            _overlay.querySelector('#fm-close-x').addEventListener('click', FormModal.close);
            _overlay.querySelector('#fm-cancel').addEventListener('click', FormModal.close);
            _overlay.querySelector('#fm-submit').addEventListener('click', function () {
                doSubmit(config, fields);
            });

            /* Live validation on blur */
            fields.forEach(function (field) {
                if (field.type === 'checkbox' || field.type === 'searchable-select') return;
                var el = _overlay.querySelector('[name="' + field.name + '"]');
                if (el) el.addEventListener('blur', function () { validateField(field, el); });
            });

            var first = _overlay.querySelector('.fm-field__input, .fm-field__textarea');
            if (first) setTimeout(function () { first.focus(); }, 80);
        },

        close: function () {
            if (!_overlay) return;
            _overlay.classList.add('fm-overlay--hidden');
            _overlay.innerHTML = '';
            document.body.style.overflow = '';
            _config = null;
        }
    };
})();
