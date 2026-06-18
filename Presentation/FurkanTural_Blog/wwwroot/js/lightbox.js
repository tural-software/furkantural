// =============================================================================
// lightbox.js — generic görsel inceleme (tüm site)
// Sitedeki içerik görsellerine tıklanınca görseli büyük, odaklı bir modalda açar.
// Kapatma: 'X' butonu · görsel DIŞINA (arka plana) tıklama · ESC.
// Modal yalnız görseli içerir (başka içerik yok).
//
// Generic yaklaşım — event delegation: tek bir document click dinleyicisi tüm
// <img>'leri (gelecekte dinamik eklenenler dahil) yakalar. Hariç tutulanlar:
//   - <a>/<button> içindeki görseller (link/buton önceliği)
//   - [data-no-lightbox] (öğenin kendisi ya da bir atası)
//   - doğal genişliği < 48px olan küçük ikonlar
// Büyük kaynak için isteğe bağlı `data-full` özniteliği desteklenir.
//
// Erişilebilirlik (WCAG 2.1.1 Klavye): uygun görseller focusable yapılır
// (tabindex/role/aria-label); Enter/Space ile açılır; modal açılınca odak kapat
// butonuna taşınır ve Tab ile modal içinde kalır (focus-trap); kapanışta odak
// tetikleyen görsele geri verilir.
// =============================================================================
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

    // Arka plana (görsel dışına) tıklama kapatır; görsele tıklama kapatmaz.
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
    document.documentElement.classList.add('lightbox-open'); // sayfa kaydırmasını kilitle
    isOpen = true;
    closeBtn.focus(); // odağı modala taşı
  }

  function close() {
    if (!overlay || !isOpen) return;
    overlay.classList.remove('is-open');
    overlay.setAttribute('aria-hidden', 'true');
    document.documentElement.classList.remove('lightbox-open');
    isOpen = false;
    if (lastFocused && typeof lastFocused.focus === 'function') {
      lastFocused.focus(); // odağı tetikleyen öğeye geri ver
    }
    lastFocused = null;
  }

  function eligible(img) {
    if (!img || img.tagName !== 'IMG') return false;
    if (img.closest('a, button')) return false;
    if (img.closest('[data-no-lightbox]')) return false;
    if (img.closest('.lightbox')) return false;
    var w = img.naturalWidth || img.width || 0;
    if (w && w < 48) return false;             // küçük ikonları atla (yüklendiğinde geçerli)
    return true;
  }

  function openFromImg(img) {
    open(img.getAttribute('data-full') || img.currentSrc || img.src, img.alt, img);
  }

  // Tıklama — event delegation (tüm görseller, gelecekte eklenenler dahil).
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
      // Modal açık: ESC kapatır; Tab odağı modal içinde tutar (tek öğe = kapat).
      if (e.key === 'Escape') { close(); return; }
      if (e.key === 'Tab') { e.preventDefault(); closeBtn.focus(); }
      return;
    }
    // Modal kapalı: odaklı zoomable görselde Enter/Space açar.
    if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar') {
      var el = document.activeElement;
      if (el && el.classList && el.classList.contains('lightbox-zoomable') && eligible(el)) {
        e.preventDefault();
        openFromImg(el);
      }
    }
  });

  // Uygun görselleri zoom-in imleci + klavye erişimi için işaretle.
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
