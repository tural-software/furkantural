// Section scroll-reveal — IntersectionObserver ile görünüme girince yumuşak giriş.
// SEO/a11y güvenli: içerik DOM'da ve yalnızca opacity/transform ile gizlenir
// (display/visibility DEĞİL, ekran okuyucu görür). Gizleme CSS'te `.js .reveal`
// ile koşullu → JS çalışmazsa içerik tam görünür. Reduced-motion'da CSS daima
// görünür bırakır, bu script de hiçbir şey yapmaz.
(function () {
  'use strict';

  var els = document.querySelectorAll('.reveal');
  if (!els.length) return;

  // Hareket azaltma tercihi → CSS zaten görünür bırakıyor; dokunma.
  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  // IntersectionObserver yoksa → hepsini hemen göster (güvenli fallback).
  if (!('IntersectionObserver' in window)) {
    for (var i = 0; i < els.length; i++) els[i].classList.add('is-visible');
    return;
  }

  var io = new IntersectionObserver(function (entries, obs) {
    entries.forEach(function (en) {
      // Öğenin %12'si görünür OLDUĞUNDA göster. Ama öğe viewport'tan uzunsa
      // intersectionRatio asla %12'ye ulaşamaz; bu durumda görünüme girer girmez
      // (ratio 0 eşiği) göster — yoksa opacity:0'da takılı kalır. rootBounds null
      // olabilir → window.innerHeight'a düş.
      if (!en.isIntersecting) return;
      var rootH = en.rootBounds ? en.rootBounds.height : window.innerHeight;
      if (en.intersectionRatio >= 0.12 || en.boundingClientRect.height > rootH) {
        en.target.classList.add('is-visible');
        obs.unobserve(en.target);
      }
    });
  }, { threshold: [0, 0.12], rootMargin: '0px 0px -10% 0px' });

  els.forEach(function (el) { io.observe(el); });
})();
