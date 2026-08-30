() => {
  const SKIP_TAGS = ['script', 'style', 'noscript', 'template', 'br', 'meta', 'link'];

  function name(el) {
    let s = el.tagName.toLowerCase();
    if (el.id) return s + '#' + el.id;
    const cls = typeof el.className === 'string' ? el.className.trim() : '';
    if (cls) return s + '.' + cls.split(/\s+/).join('.');
    return s;
  }

  function path(el) {
    const parts = [];
    let node = el;
    while (node && node !== document.body && parts.length < 4) {
      parts.unshift(name(node));
      node = node.parentElement;
    }
    return parts.join(' > ');
  }

  const rows = [];
  for (const el of document.querySelectorAll('body *')) {
    const tag = el.tagName.toLowerCase();
    if (SKIP_TAGS.indexOf(tag) >= 0) continue;
    if (el.closest('.cf-turnstile')) continue;
    if (!el.id && !(typeof el.className === 'string' && el.className.trim())) continue;

    const st = getComputedStyle(el);
    if (st.display === 'none' || st.visibility === 'hidden') continue;

    const r = el.getBoundingClientRect();
    if (r.width === 0 && r.height === 0) continue;

    rows.push(path(el) + '  ' +
      Math.round(r.left) + ',' + Math.round(r.top + scrollY) + ',' +
      Math.round(r.width) + ',' + Math.round(r.height));
  }

  return rows.join('\n');
}
