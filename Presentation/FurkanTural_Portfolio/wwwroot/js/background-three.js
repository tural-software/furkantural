// Portfolio — tüm-sayfa sabit three.js parçacık/constellation arkaplanı (#canvas-container).
// Guard'lar: prefers-reduced-motion, WebGL yokluğu ve modül yükleme hatası →
// hepsinde sessizce çık; statik içerik (solid bg) bozulmadan kalır.
(function () {
  'use strict';

  var canvas = document.getElementById('canvas-container');
  if (!canvas) return;

  // 1) Hareket azaltma tercihi → sahneyi hiç başlatma (a11y).
  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  // 2) WebGL desteğini AYRI bir canvas'ta test et — gerçek canvas'ın context tipini
  //    kirletmeyelim (three kendi webgl/webgl2 context'ini açacak).
  function webglSupported() {
    try {
      var probe = document.createElement('canvas');
      return !!(window.WebGLRenderingContext &&
        (probe.getContext('webgl') || probe.getContext('experimental-webgl')));
    } catch (e) { return false; }
  }
  if (!webglSupported()) return;

  // 3) three.js'i CDN'den sürüm-sabitli (pinned) dinamik import ile yükle.
  //    Not: dinamik import() SRI desteklemez; bütünlük için sürüm sabitlenmiştir.
  var THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.min.js';

  import(THREE_URL)
    .then(function (THREE) { initScene(THREE); })
    .catch(function () { /* yükleme başarısız → statik hero korunur */ });

  function initScene(THREE) {
    var renderer;
    try {
      renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true, alpha: true });
    } catch (e) { return; }

    var DPR = Math.min(window.devicePixelRatio || 1, 1.5);
    renderer.setPixelRatio(DPR);

    var scene = new THREE.Scene();
    var camera = new THREE.PerspectiveCamera(70, 1, 1, 1000);
    camera.position.z = 320;

    // Parçacık sayısı ekran genişliğine göre (mobilde düşük tut).
    var count = window.innerWidth < 768 ? 70 : 150;
    var positions = new Float32Array(count * 3);
    for (var i = 0; i < count; i++) {
      positions[i * 3]     = (Math.random() - 0.5) * 600;
      positions[i * 3 + 1] = (Math.random() - 0.5) * 400;
      positions[i * 3 + 2] = (Math.random() - 0.5) * 400;
    }
    var geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

    var material = new THREE.PointsMaterial({
      color: 0x38bdf8,           // site accent (Admin paleti)
      size: 2.4,
      transparent: true,
      opacity: 0.7,
      sizeAttenuation: true,
      depthWrite: false
    });
    var points = new THREE.Points(geometry, material);

    // Parçacıkları ve bağlantı çizgilerini tek grupta döndür; böylece çizgiler
    // statik (yerel) konumlardan BİR KEZ hesaplanır → kare-başına ek maliyet yok.
    var group = new THREE.Group();
    group.add(points);

    // Constellation: yakın parçacıkları ince çizgilerle bağla (seyrek, zarif).
    var THRESH = 72, THRESH2 = THRESH * THRESH, MAX_SEG = 320;
    var linePos = [];
    for (var a = 0; a < count; a++) {
      var ax = positions[a * 3], ay = positions[a * 3 + 1], az = positions[a * 3 + 2];
      for (var b = a + 1; b < count; b++) {
        var dx = ax - positions[b * 3], dy = ay - positions[b * 3 + 1], dz = az - positions[b * 3 + 2];
        if (dx * dx + dy * dy + dz * dz < THRESH2) {
          linePos.push(ax, ay, az, positions[b * 3], positions[b * 3 + 1], positions[b * 3 + 2]);
        }
      }
      if (linePos.length >= MAX_SEG * 6) break;
    }
    if (linePos.length) {
      var lineGeo = new THREE.BufferGeometry();
      lineGeo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(linePos), 3));
      var lineMat = new THREE.LineBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.12, depthWrite: false });
      group.add(new THREE.LineSegments(lineGeo, lineMat));
    }

    scene.add(group);

    var raf = null;
    var mouseX = 0, mouseY = 0;
    var t0 = performance.now();

    function resize() {
      var w = canvas.clientWidth, h = canvas.clientHeight;
      if (!w || !h) return;
      renderer.setSize(w, h, false);
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
    }

    function frame(now) {
      var t = (now - t0) * 0.0001;
      group.rotation.y = t;
      group.rotation.x = t * 0.4;
      // Hafif fare parallax'ı yumuşatarak uygula.
      camera.position.x += (mouseX * 40 - camera.position.x) * 0.04;
      camera.position.y += (-mouseY * 40 - camera.position.y) * 0.04;
      camera.lookAt(scene.position);
      renderer.render(scene, camera);
      raf = requestAnimationFrame(frame);
    }

    function start() { if (raf === null) raf = requestAnimationFrame(frame); }
    function stop()  { if (raf !== null) { cancelAnimationFrame(raf); raf = null; } }

    resize();
    window.addEventListener('resize', resize);
    window.addEventListener('pointermove', function (e) {
      mouseX = (e.clientX / window.innerWidth) - 0.5;
      mouseY = (e.clientY / window.innerHeight) - 0.5;
    }, { passive: true });

    // Sekme gizliyken duraklat (pil/CPU tasarrufu).
    document.addEventListener('visibilitychange', function () {
      if (document.hidden) stop(); else start();
    });

    // Hero ekran dışındayken duraklat.
    if ('IntersectionObserver' in window) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (en) { if (en.isIntersecting) start(); else stop(); });
      }, { threshold: 0.01 });
      io.observe(canvas);
    } else {
      start();
    }

    canvas.classList.add('is-ready');
    start();
  }
})();
