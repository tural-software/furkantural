// İçerik görsellerini büyük bir modalda açar. Tek tek bağlanmaz: belge düzeyinde tek bir tıklama
// dinleyicisi çalışır, böylece sonradan eklenen görseller de kendiliğinden kapsama girer.
//
// Kapsam dışı kalanlar: bağlantı veya düğme içindeki görseller, [data-no-lightbox] taşıyan ya da
// böyle bir atası olan öğeler, ve doğal genişliği 48 pikselden küçük olan ikonlar. Görsel büyük bir
// sürümünü `data-full` ile bildirebilir; bildirmezse gösterilen kaynak kullanılır.
//
// Klavyeyle de açılır: uygun görseller odaklanabilir yapılır, Enter ve boşluk modalı açar. Modal
// açıkken odak içeride kalır, kapanışta tetikleyen görsele döner. Bu davranış kaldırılırsa modal
// yalnızca fareyle kullanılabilir hâle gelir.
(function () {
  'use strict';

  var overlay = null, imgEl = null, closeBtn = null, isOpen = false, lastFocused = null;

  function build() {
    overlay = document.createElement('div');
    overlay.className = 'lightbox';
    overlay.setAttribute('role', 'dialog');
    overlay.setAttribute('aria-modal', 'true');
    overlay.setAttribute('aria-hidden', 'true');
    overlay.innerHTML =
      '<button type="button" class="lightbox__close" aria-label="Kapat">✕</button>' +
      '<img class="lightbox__img" alt="" />';
    imgEl = overlay.querySelector('.lightbox__img');
    closeBtn = overlay.querySelector('.lightbox__close');
    document.body.appendChild(overlay);

    overlay.addEventListener('click', function (e) {
      if (e.target === imgEl) return;
      close();
    });
    closeBtn.addEventListener('click', close);
  }

  function open(src, alt, trigger) {
    if (!src) return;
    if (!overlay) build();
    lastFocused = trigger || (document.activeElement !== document.body ? document.activeElement : null);
    imgEl.setAttribute('src', src);
    imgEl.setAttribute('alt', alt || '');
    overlay.classList.add('is-open');
    overlay.setAttribute('aria-hidden', 'false');
    document.documentElement.classList.add('lightbox-open');
    isOpen = true;
    closeBtn.focus();
  }

  function close() {
    if (!overlay || !isOpen) return;
    overlay.classList.remove('is-open');
    overlay.setAttribute('aria-hidden', 'true');
    document.documentElement.classList.remove('lightbox-open');
    isOpen = false;
    if (lastFocused && typeof lastFocused.focus === 'function') {
      lastFocused.focus();
    }
    lastFocused = null;
  }

  function eligible(img) {
    if (!img || img.tagName !== 'IMG') return false;
    if (img.closest('a, button')) return false;
    if (img.closest('[data-no-lightbox]')) return false;
    if (img.closest('.lightbox')) return false;
    var w = img.naturalWidth || img.width || 0;
    if (w && w < 48) return false;
    return true;
  }

  function openFromImg(img) {
    open(img.getAttribute('data-full') || img.currentSrc || img.src, img.alt, img);
  }

  document.addEventListener('click', function (e) {
    var t = e.target;
    if (!t || !t.closest) return;
    var img = t.closest('img');
    if (!eligible(img)) return;
    e.preventDefault();
    openFromImg(img);
  });

  document.addEventListener('keydown', function (e) {
    if (isOpen) {
      if (e.key === 'Escape') { close(); return; }
      if (e.key === 'Tab') { e.preventDefault(); closeBtn.focus(); }
      return;
    }
    if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
      var el = document.activeElement;
      if (el && el.classList && el.classList.contains('lightbox-zoomable') && eligible(el)) {
        e.preventDefault();
        openFromImg(el);
      }
    }
  });

  function markZoomable(img) {
    img.classList.add('lightbox-zoomable');
    img.setAttribute('tabindex', '0');
    img.setAttribute('role', 'button');
    if (!img.getAttribute('aria-label')) {
      img.setAttribute('aria-label', (img.alt ? img.alt + ', ' : '') + 'görseli büyüt');
    }
  }

  document.addEventListener('DOMContentLoaded', function () {
    Array.prototype.forEach.call(document.images, function (img) {
      if (eligible(img)) markZoomable(img);
    });
  });
})();
