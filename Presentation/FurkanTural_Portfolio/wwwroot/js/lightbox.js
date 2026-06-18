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
// =============================================================================
(function () {
  'use strict';

  var overlay = null, imgEl = null, isOpen = false;

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
    document.body.appendChild(overlay);

    // Arka plana (görsel dışına) tıklama kapatır; görsele tıklama kapatmaz.
    overlay.addEventListener('click', function (e) {
      if (e.target === imgEl) return;
      close();
    });
    overlay.querySelector('.lightbox__close').addEventListener('click', close);
  }

  function open(src, alt) {
    if (!src) return;
    if (!overlay) build();
    imgEl.setAttribute('src', src);
    imgEl.setAttribute('alt', alt || '');
    overlay.classList.add('is-open');
    overlay.setAttribute('aria-hidden', 'false');
    document.documentElement.classList.add('lightbox-open'); // sayfa kaydırmasını kilitle
    isOpen = true;
  }

  function close() {
    if (!overlay || !isOpen) return;
    overlay.classList.remove('is-open');
    overlay.setAttribute('aria-hidden', 'true');
    document.documentElement.classList.remove('lightbox-open');
    isOpen = false;
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

  // Tıklama — event delegation (tüm görseller, gelecekte eklenenler dahil).
  document.addEventListener('click', function (e) {
    var t = e.target;
    if (!t || !t.closest) return;
    var img = t.closest('img');
    if (!eligible(img)) return;
    e.preventDefault();
    open(img.getAttribute('data-full') || img.currentSrc || img.src, img.alt);
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && isOpen) close();
  });

  // Uygun görsellere zoom-in imleci işaretle (görsel-içi ipucu).
  document.addEventListener('DOMContentLoaded', function () {
    Array.prototype.forEach.call(document.images, function (img) {
      if (eligible(img)) img.classList.add('lightbox-zoomable');
    });
  });
})();
