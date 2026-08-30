() => {
  const MAX_ITEMS = 25;

  function describe(el) {
    if (!el || el.nodeType !== 1) return '?';
    let s = el.tagName.toLowerCase();
    if (el.id) return s + '#' + el.id;
    const cls = typeof el.className === 'string' ? el.className.trim() : '';
    if (cls) s += '.' + cls.split(/\s+/).slice(0, 3).join('.');
    return s;
  }

  function visible(el, st) {
    if (st.visibility === 'hidden' || st.display === 'none') return false;
    const r = el.getBoundingClientRect();
    return r.width > 0 || r.height > 0;
  }

  function parseColor(c) {
    const m = String(c).match(/rgba?\(([^)]+)\)/);
    if (!m) return null;
    const p = m[1].split(/[,\s/]+/).filter(x => x.length).map(parseFloat);
    if (p.length < 3 || p.some(isNaN)) return null;
    return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 };
  }

  function over(fg, bg) {
    const a = fg.a;
    return { r: fg.r * a + bg.r * (1 - a), g: fg.g * a + bg.g * (1 - a), b: fg.b * a + bg.b * (1 - a), a: 1 };
  }

  function luminance(c) {
    const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
    return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
  }

  function contrast(a, b) {
    const l1 = luminance(a), l2 = luminance(b);
    return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
  }

  function effectiveBackground(el) {
    const layers = [];
    let node = el;
    while (node && node.nodeType === 1) {
      const st = getComputedStyle(node);
      if (st.backgroundImage && st.backgroundImage !== 'none') return { unmeasurable: 'background-image' };
      if (parseFloat(st.opacity) < 1) return { unmeasurable: 'opacity' };
      const c = parseColor(st.backgroundColor);
      if (c && c.a > 0) {
        layers.push(c);
        if (c.a >= 1) {
          let acc = { r: 255, g: 255, b: 255, a: 1 };
          for (let i = layers.length - 1; i >= 0; i--) acc = over(layers[i], acc);
          return { color: acc };
        }
      }
      node = node.parentElement;
    }
    return { unmeasurable: 'no-opaque-backdrop' };
  }

  function scrollableAncestor(el) {
    let node = el.parentElement;
    while (node && node !== document.body) {
      const ox = getComputedStyle(node).overflowX;
      if (ox === 'auto' || ox === 'scroll' || ox === 'hidden') return true;
      node = node.parentElement;
    }
    return false;
  }

  function accessibleName(el) {
    if (el.getAttribute('aria-label')) return true;
    const lb = el.getAttribute('aria-labelledby');
    if (lb && lb.split(/\s+/).some(id => document.getElementById(id))) return true;
    if (el.getAttribute('title')) return true;
    const tag = el.tagName.toLowerCase();
    if ((tag === 'button' || el.getAttribute('role') === 'button') && (el.textContent || '').trim()) return true;
    if (el.id && document.querySelector('label[for="' + CSS.escape(el.id) + '"]')) return true;
    if (el.closest('label')) return true;
    if (el.getAttribute('placeholder')) return true;
    return false;
  }

  function inSentence(el) {
    const parent = el.parentElement;
    if (!parent) return false;
    if (getComputedStyle(el).display !== 'inline') return false;
    const own = (el.textContent || '').trim();
    const around = (parent.textContent || '').trim();
    return around.length > own.length;
  }

  function centre(r) {
    return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
  }

  function circleHitsRect(c, r) {
    const nx = Math.max(r.left, Math.min(c.x, r.right));
    const ny = Math.max(r.top, Math.min(c.y, r.bottom));
    return Math.hypot(c.x - nx, c.y - ny) < 12;
  }

  function spacedApart(target, all) {
    const c = centre(target.rect);
    for (const other of all) {
      if (other === target) continue;
      const undersized = Math.min(other.rect.width, other.rect.height) < 24;
      if (undersized) {
        const oc = centre(other.rect);
        if (Math.hypot(c.x - oc.x, c.y - oc.y) < 24) return false;
      } else if (circleHitsRect(c, other.rect)) {
        return false;
      }
    }
    return true;
  }

  const ALLOWED_SCROLLERS = [
    '.prose pre', '.prose table', '.dm-code', '.data-table-wrap', '.code-block', 'pre'
  ];

  function allowedScroller(el) {
    return ALLOWED_SCROLLERS.some(sel => {
      try { return el.matches(sel); } catch (e) { return false; }
    });
  }

  function collectScrollers() {
    const found = [];
    const candidates = [document.documentElement, document.body]
      .concat(Array.from(document.querySelectorAll('body *')));

    for (const el of candidates) {
      if (!el || found.length >= MAX_ITEMS) continue;
      const overflowX = getComputedStyle(el).overflowX;
      const isRoot = el === document.documentElement || el === document.body;
      const scrollsSideways = overflowX === 'auto' || overflowX === 'scroll';
      if (!isRoot && !scrollsSideways) continue;
      if (el.scrollWidth <= el.clientWidth + 1) continue;
      if (allowedScroller(el)) continue;
      found.push(describe(el) + ' ' + el.clientWidth + ' -> ' + el.scrollWidth +
        ' (overflow-x: ' + overflowX + ')');
    }
    return found;
  }

  const de = document.documentElement;
  const viewportWidth = de.clientWidth;

  const targets = [];
  const overflowers = [];
  const smallTargets = [];
  const missingAlt = [];
  const unlabelled = [];
  const namelessLinks = [];
  const lowContrast = [];
  const unmeasurable = [];
  const headings = [];

  for (const el of document.querySelectorAll('body *')) {
    const st = getComputedStyle(el);
    if (!visible(el, st)) continue;
    const rect = el.getBoundingClientRect();

    if ((rect.right > viewportWidth + 1 || rect.left < -1) && !scrollableAncestor(el)) {
      if (overflowers.length < MAX_ITEMS) {
        overflowers.push(describe(el) + ' [' + Math.round(rect.left) + '..' + Math.round(rect.right) + ']');
      }
    }

    const tag = el.tagName.toLowerCase();

    if (/^h[1-6]$/.test(tag)) {
      headings.push({ level: Number(tag[1]), text: (el.textContent || '').trim().slice(0, 60) });
    }

    if (tag === 'img' && !el.hasAttribute('alt') && missingAlt.length < MAX_ITEMS) {
      missingAlt.push(describe(el) + ' src=' + String(el.getAttribute('src') || '').slice(-40));
    }

    const inputType = String(el.type || '').toLowerCase();
    const isControl = tag === 'button' || tag === 'select' || tag === 'textarea' ||
      (tag === 'input' && ['hidden', 'submit', 'button', 'reset', 'image'].indexOf(inputType) === -1);
    if (isControl && !accessibleName(el) && unlabelled.length < MAX_ITEMS) {
      unlabelled.push(describe(el));
    }

    if (tag === 'a' && !(el.textContent || '').trim() && !accessibleName(el) && namelessLinks.length < MAX_ITEMS) {
      namelessLinks.push(describe(el) + ' href=' + String(el.getAttribute('href') || '').slice(0, 40));
    }

    const isTarget = tag === 'button' || tag === 'select' || el.getAttribute('role') === 'button' ||
      (tag === 'input' && (inputType === 'checkbox' || inputType === 'radio')) ||
      (tag === 'a' && el.hasAttribute('href'));
    if (isTarget && !inSentence(el)) targets.push({ el: el, rect: rect });
  }

  for (const t of targets) {
    if (Math.min(t.rect.width, t.rect.height) >= 24) continue;
    if (spacedApart(t, targets)) continue;
    if (smallTargets.length < MAX_ITEMS) {
      smallTargets.push(describe(t.el) + ' ' + Math.round(t.rect.width) + 'x' + Math.round(t.rect.height));
    }
  }

  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  const seen = {};
  let node;
  while ((node = walker.nextNode())) {
    const text = (node.nodeValue || '').trim();
    if (!text) continue;
    const el = node.parentElement;
    if (!el) continue;
    const tag = el.tagName.toLowerCase();
    if (tag === 'script' || tag === 'style' || tag === 'noscript' || tag === 'title') continue;
    const st = getComputedStyle(el);
    if (!visible(el, st)) continue;
    if (st.webkitTextFillColor && st.webkitTextFillColor !== st.color) continue;

    const key = describe(el) + '|' + st.color + '|' + st.fontSize + '|' + st.fontWeight;
    if (seen[key]) continue;
    seen[key] = true;

    const fg = parseColor(st.color);
    if (!fg || fg.a === 0) continue;
    const bg = effectiveBackground(el);
    if (bg.unmeasurable) {
      if (unmeasurable.length < MAX_ITEMS) unmeasurable.push(describe(el) + ' (' + bg.unmeasurable + ')');
      continue;
    }

    const size = parseFloat(st.fontSize);
    const weight = Number(st.fontWeight) || 400;
    const large = size >= 24 || (size >= 18.66 && weight >= 700);
    const required = large ? 3.0 : 4.5;
    const actual = contrast(fg.a < 1 ? over(fg, bg.color) : fg, bg.color);
    if (actual + 0.005 < required && lowContrast.length < MAX_ITEMS) {
      lowContrast.push(describe(el) + ' ' + actual.toFixed(2) + ':1 < ' + required.toFixed(1) +
        ' (' + Math.round(size) + 'px/' + weight + ') "' + text.slice(0, 30) + '"');
    }
  }

  const idCounts = {};
  const duplicateIds = [];
  for (const el of document.querySelectorAll('[id]')) {
    const id = el.id;
    idCounts[id] = (idCounts[id] || 0) + 1;
    if (idCounts[id] === 2 && duplicateIds.length < MAX_ITEMS) duplicateIds.push(id);
  }

  return {
    viewportWidth: viewportWidth,
    scrollWidth: de.scrollWidth,
    clientWidth: de.clientWidth,
    lang: de.getAttribute('lang') || '',
    title: (document.title || '').trim(),
    theme: de.getAttribute('data-theme') || '',
    scrollers: collectScrollers(),
    h1Count: headings.filter(h => h.level === 1).length,
    headings: headings,
    overflowers: overflowers,
    smallTargets: smallTargets,
    missingAlt: missingAlt,
    unlabelled: unlabelled,
    namelessLinks: namelessLinks,
    lowContrast: lowContrast,
    unmeasurable: unmeasurable,
    duplicateIds: duplicateIds
  };
}
