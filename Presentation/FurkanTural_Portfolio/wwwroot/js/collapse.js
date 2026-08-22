// Uzun listeleri katlayıp "Devamını Göster" düğmesi arkasına alır. Davranışı kapsayıcıdaki
// data-collapse değeri seçer:
//
//   row → ızgaralar için. Görünür öğe sayısı o anda ekrana sığan sütun sayısı kadardır, yani tam
//         bir satır. İçerik zaten tek satıra sığıyorsa düğme hiç çıkmaz. Sütun sayısı ekran
//         genişliğiyle değiştiği için yeniden boyutlandırmada baştan hesaplanır; dar ekranda tek
//         sütuna inildiğinde taban en az MOBILE_FLOOR öğedir, aksi hâlde tek kart görünüp kalan
//         her şey gizlenirdi.
//   N   → dikey listeler için sabit sayı. Satır kavramı tek sütunlu bir listede karşılık
//         bulmadığından burada sütun hesabı yapılmaz.
//
// Gizleme yalnızca betikle yapılır ve içerik her zaman belgede durur. Betik çalışmazsa hiçbir şey
// gizlenmez; arama motoru ve ekran okuyucu tamamını görür, dolayısıyla bu bir gizleme hilesi
// değildir. Hareket azaltma tercihi açıkken açılış animasyonu ve yumuşak kaydırma uygulanmaz.
(function () {
  'use strict';

  // Buton etiketleri _Layout'taki <main> üzerinden lokalize gelir (SharedLocalizer);
  // bulunamazsa Türkçe varsayılana düşer (site tr-TR).
  var main = document.getElementById('main');
  var MORE = (main && main.getAttribute('data-collapse-more')) || 'Devamını Göster';
  var LESS = (main && main.getAttribute('data-collapse-less')) || 'Daha Az Göster';
  var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // Mobilde (≤768px, sitenin breakpoint'i) grid tek sütuna iner → "1 satır = 1 kart"
  // çok agresif olur; bu yüzden mobilde EN AZ bu kadar öğe gösterilir. mqMobile.matches
  // her çağrıda okunduğundan resize'da da geçerlidir.
  var MOBILE_FLOOR = 3;
  var mqMobile = window.matchMedia ? window.matchMedia('(max-width: 768px)') : null;
  function isMobile() { return !!(mqMobile && mqMobile.matches); }

  var uid = 0;
  var states = [];

  // Grid'in o anki sütun sayısı = çözümlenmiş grid-template-columns track adedi.
  // auto-fill az öğede bile boş track'leri sayar → "satır başına kapasite"yi verir.
  // Grid değilse (örn. timeline) 'none' döner → 0.
  function columnsOf(grid) {
    var template = window.getComputedStyle(grid).getPropertyValue('grid-template-columns');
    if (!template || template === 'none') return 0;
    return template.split(/\s+/).filter(Boolean).length;
  }

  function showItem(el) { el.classList.remove('collapse-item--hidden'); }
  function hideItem(el) { el.classList.remove('collapse-item--in'); el.classList.add('collapse-item--hidden'); }

  // O anki görünür öğe limiti (row modunda sütun sayısı, fixed modunda sabit N).
  function limitOf(st) {
    if (st.mode === 'row') {
      var cols = columnsOf(st.container);
      if (cols <= 0) return st.items.length;                       // grid çözülemezse hiç gizleme
      if (isMobile() && cols < MOBILE_FLOOR) return MOBILE_FLOOR;  // mobil tek-sütun → en az 3
      return cols;
    }
    return st.fixed;
  }

  // Mevcut limite + açık/kapalı duruma göre öğeleri ve butonu güncelle.
  function refresh(st) {
    var limit = limitOf(st);
    var total = st.items.length;

    if (total <= limit) {                          // tek satıra/limite sığıyor → buton yok
      st.items.forEach(showItem);
      st.wrap.hidden = true;
      return;
    }
    st.wrap.hidden = false;

    if (st.expanded) {
      st.items.forEach(showItem);
      st.btn.textContent = LESS;
      st.btn.setAttribute('aria-expanded', 'true');
    } else {
      st.items.forEach(function (el, i) { if (i < limit) showItem(el); else hideItem(el); });
      st.btn.textContent = MORE + ' (' + (total - limit) + ')';
      st.btn.setAttribute('aria-expanded', 'false');
    }
  }

  Array.prototype.forEach.call(document.querySelectorAll('[data-collapse]'), function (container) {
    var attr = container.getAttribute('data-collapse');
    var mode, fixed = 0;
    if (attr === 'row') {
      mode = 'row';
    } else {
      fixed = parseInt(attr, 10);
      if (!fixed || fixed < 1) return;
      mode = 'fixed';
    }

    // Yalnız element çocuklar "öğe" sayılır (whitespace text node'ları hariç).
    var items = Array.prototype.filter.call(container.children, function (el) { return el.nodeType === 1; });
    if (!items.length) return;

    if (!container.id) container.id = 'collapsible-' + (++uid);

    var wrap = document.createElement('div');
    wrap.className = 'collapse-toggle-wrap';
    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'btn btn-outline collapse-toggle';
    btn.setAttribute('aria-controls', container.id);
    wrap.appendChild(btn);
    container.parentNode.insertBefore(wrap, container.nextSibling);

    var st = { container: container, items: items, wrap: wrap, btn: btn, mode: mode, fixed: fixed, expanded: false };
    states.push(st);

    btn.addEventListener('click', function () {
      st.expanded = !st.expanded;
      if (st.expanded) {
        if (!reduce) {
          var limit = limitOf(st);
          st.items.forEach(function (el, i) { if (i >= limit) el.classList.add('collapse-item--in'); });
        }
      } else {
        // Katlayınca kullanıcı listenin çok altında asılı kalmasın → section başına dön.
        var section = st.container.closest('.section');
        if (section) section.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' });
      }
      refresh(st);
    });

    refresh(st);
  });

  // Ekran genişliği değişince satır başına sütun sayısı değişebilir → "row" modundaki
  // section'ları yeniden değerlendir (debounced). Kullanıcı açtıysa açık kalır.
  if (states.some(function (s) { return s.mode === 'row'; })) {
    var t;
    window.addEventListener('resize', function () {
      clearTimeout(t);
      t = setTimeout(function () {
        states.forEach(function (s) { if (s.mode === 'row') refresh(s); });
      }, 150);
    });
  }
})();
