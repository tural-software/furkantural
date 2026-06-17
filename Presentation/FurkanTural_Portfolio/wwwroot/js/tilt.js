// Kartlara hafif 3B pointer-tilt (yetenek + proje kartları).
// Guard'lar: prefers-reduced-motion VE dokunmatik/coarse-pointer cihazlarda
// tamamen devre dışı — yalnız ince işaretçi + hover olan cihazlarda çalışır.
(function () {
  'use strict';

  if (window.matchMedia) {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    if (window.matchMedia('(hover: none), (pointer: coarse)').matches) return;
  }

  var cards = document.querySelectorAll('.skill-card, .project-card');
  if (!cards.length) return;

  var MAX = 6; // azami eğim (derece)

  cards.forEach(function (card) {
    // Hareket sırasında işaretçiyi anında takip et (CSS transform-transition'ı geçici kapat).
    card.addEventListener('pointerenter', function () { card.style.transition = 'transform 0s'; });

    card.addEventListener('pointermove', function (e) {
      var r = card.getBoundingClientRect();
      if (!r.width || !r.height) return;
      var px = (e.clientX - r.left) / r.width;   // 0..1
      var py = (e.clientY - r.top) / r.height;   // 0..1
      var ry = (px - 0.5) * 2 * MAX;             // sol/sağ → Y ekseni
      var rx = (0.5 - py) * 2 * MAX;             // üst/alt → X ekseni
      card.style.transform = 'perspective(700px) rotateX(' + rx.toFixed(2) + 'deg) rotateY(' + ry.toFixed(2) + 'deg)';
    });

    card.addEventListener('pointerleave', function () {
      card.style.transition = '';   // CSS transition'a dön → yumuşak reset
      card.style.transform = '';
    });
  });
})();
