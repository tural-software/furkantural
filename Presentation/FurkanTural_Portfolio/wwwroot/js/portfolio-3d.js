// =============================================================================
// portfolio-3d.js
// Hero imza objesi (#ftHero) + müzik 3D visualizer (#ftMusic) + perspektif tilt
// ([data-tilt]). Mevcut background-three.js ile AYNI guard desenini izler:
//   - prefers-reduced-motion → 3D hiç başlamaz (statik içerik korunur)
//   - WebGL yoksa / three yüklenmezse → sessizce çık (try/catch)
//   - sekme gizliyken + öğe ekran dışındayken rAF duraklatılır (pil/CPU)
// three.js, mevcut background-three.js ile aynı sürümden (pinned) yüklenir;
// import() tekrarlanınca tarayıcı modülü yeniden değerlendirmez (tek fetch).
// =============================================================================
(function () {
  'use strict';

  // 1) Hareket azaltma tercihi → hiçbir 3D başlatma. Tilt yine de çalışır ama
  //    geçişsiz (kart hover'ı görsel olarak kalır, animasyon yok).
  var REDUCED = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // 2) WebGL desteğini ayrı bir probe canvas'ta test et (asıl canvas'ı kirletme).
  function webglSupported() {
    try {
      var p = document.createElement('canvas');
      return !!(window.WebGLRenderingContext && (p.getContext('webgl') || p.getContext('experimental-webgl')));
    } catch (e) { return false; }
  }

  var THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.min.js';

  document.addEventListener('DOMContentLoaded', function () {
    initTilt();                 // saf CSS 3D — WebGL gerektirmez
    if (REDUCED || !webglSupported()) return;
    import(THREE_URL)
      .then(function (THREE) {
        try { initHero(THREE); } catch (e) {}
        try { initVisualizer(THREE); } catch (e) {}
      })
      .catch(function () { /* yükleme başarısız → statik içerik korunur */ });
  });

  // --- ortak: sekme gizliyken duraklat + ekran dışında duraklat (IO) ----------
  function runLoop(canvas, render) {
    var raf = null;
    var visible = true, onscreen = true;
    function tick(now) { render(now); raf = onscreen && visible ? requestAnimationFrame(tick) : null; }
    function start() { if (raf === null && onscreen && visible) raf = requestAnimationFrame(tick); }
    function stop() { if (raf !== null) { cancelAnimationFrame(raf); raf = null; } }
    document.addEventListener('visibilitychange', function () { visible = !document.hidden; if (visible) start(); else stop(); });
    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (es) {
        es.forEach(function (e) { onscreen = e.isIntersecting; if (onscreen) start(); else stop(); });
      }, { threshold: 0.01 }).observe(canvas);
    }
    start();
  }

  function makeRenderer(THREE, canvas) {
    var r = new THREE.WebGLRenderer({ canvas: canvas, antialias: true, alpha: true });
    r.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
    return r;
  }

  // === FİKİR 1: Hero imza objesi — gerçek dokulu Dünya + yörüngede dönen Ay ===
  // NASA blue-marble dokusu (self-host: /img/earth_atmos_2048.jpg), eksen eğikliği
  // ~23.5°, yönlü güneş ışığı → gerçek gezegen gündüz/gece terminatörü. Ay ayrı bir
  // pivot grubunda dünyanın yörüngesinde dolanır. İmlece göre tüm sistem hafif eğilir.
  function initHero(THREE) {
    var c = document.getElementById('ftHero'); if (!c) return;
    var r = makeRenderer(THREE, c);
    var scene = new THREE.Scene();
    var cam = new THREE.PerspectiveCamera(45, 1, 0.1, 100); cam.position.z = 5.9;

    // Işık: yönlü "güneş" (gündüz yarımküresini aydınlatır → terminatör) + hafif mavi
    // ortam dolgusu (gece tarafı tamamen siyah kalmasın, uzayda okunur dursun).
    var sun = new THREE.DirectionalLight(0xffffff, 2.4);
    sun.position.set(-3, 1.4, 2.2); scene.add(sun);
    scene.add(new THREE.AmbientLight(0x223a52, 0.55));

    var loader = new THREE.TextureLoader();
    function tex(url) { var t = loader.load(url); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return t; }

    // Tüm sistem (dünya + ay yörüngesi) cursor parallax için tek grupta.
    var system = new THREE.Group(); scene.add(system);

    // Dünya — gerçek doku, mat yüzey (PBR). Eksen eğikliği gerçekçi görünüm verir.
    var earth = new THREE.Mesh(
      new THREE.SphereGeometry(1.2, 48, 48),
      new THREE.MeshStandardMaterial({ map: tex('/img/earth_atmos_2048.jpg'), roughness: 1, metalness: 0 })
    );
    earth.rotation.z = 0.41; // ~23.5°
    system.add(earth);

    // Ay — pivot grubu döndürülerek dünyanın çevresinde yörüngeye sokulur; yörünge
    // düzlemi hafif eğik (görsel derinlik). Ay yüzü ayrıca yavaşça döner.
    var moonPivot = new THREE.Group(); moonPivot.rotation.x = 0.42; system.add(moonPivot);
    var moon = new THREE.Mesh(
      new THREE.SphereGeometry(0.28, 32, 32),
      new THREE.MeshStandardMaterial({ map: tex('/img/moon_1024.jpg'), roughness: 1, metalness: 0 })
    );
    moon.position.x = 1.9; moonPivot.add(moon);

    var mx = 0, my = 0;
    c.addEventListener('pointermove', function (e) {
      var b = c.getBoundingClientRect();
      mx = (e.clientX - b.left) / b.width - 0.5;
      my = (e.clientY - b.top) / b.height - 0.5;
    }, { passive: true });

    function resize() { var w = c.clientWidth, h = c.clientHeight; if (!w || !h) return; r.setSize(w, h, false); cam.aspect = w / h; cam.updateProjectionMatrix(); }
    window.addEventListener('resize', resize); resize();

    runLoop(c, function () {
      earth.rotation.y += 0.0016;       // dünya kendi ekseninde döner
      moonPivot.rotation.y += 0.0068;   // ay yörüngede dolanır
      moon.rotation.y += 0.002;         // ay yüzü yavaşça döner
      // cursor parallax — sistem imlece göre yumuşakça eğilir
      system.rotation.x += (my * 0.5 - system.rotation.x) * 0.05;
      system.rotation.y += (mx * 0.5 - system.rotation.y) * 0.05;
      r.render(scene, cam);
    });
  }

  // === FİKİR 4: Müzik 3D visualizer — sky→mor bar dizisi =====================
  // İPUCU: gerçek ses analizi için Web Audio API AnalyserNode bağlanabilir;
  // şu an sahte spektrum (sin tabanlı) ile çalışır — ses dosyası gerektirmez.
  function initVisualizer(THREE) {
    var c = document.getElementById('ftMusic'); if (!c) return;
    var r = makeRenderer(THREE, c);
    var scene = new THREE.Scene();
    var cam = new THREE.PerspectiveCamera(50, 4, 0.1, 100); cam.position.set(0, 2.0, 6.2); cam.lookAt(0, 0.2, 0);

    var N = 30, bars = [], col = new THREE.Color();
    for (var i = 0; i < N; i++) {
      col.setHSL(0.55 + (i / N) * 0.16, 0.72, 0.62); // sky(#38bdf8) → mor(#a78bfa)
      var m = new THREE.Mesh(new THREE.BoxGeometry(0.2, 1, 0.2), new THREE.MeshBasicMaterial({ color: col.clone(), transparent: true, opacity: 0.92 }));
      m.position.x = (i - (N - 1) / 2) * 0.3; scene.add(m); bars.push(m);
    }

    function resize() { var w = c.clientWidth, h = c.clientHeight; if (!w || !h) return; r.setSize(w, h, false); cam.aspect = w / h; cam.updateProjectionMatrix(); }
    window.addEventListener('resize', resize); resize();

    var t0 = performance.now();
    runLoop(c, function (now) {
      var t = (now - t0) * 0.001;
      for (var i = 0; i < N; i++) {
        var sp = 1.5 + (i % 5) * 0.45;
        var v = Math.abs(Math.sin(t * sp + i * 0.5)) * Math.abs(Math.sin(t * 0.7 + i * 0.3));
        var h = 0.2 + v * 2.4;
        bars[i].scale.y = h; bars[i].position.y = h / 2 - 0.6;
      }
      scene.rotation.y = Math.sin(t * 0.2) * 0.16;
      r.render(scene, cam);
    });
  }

  // === FİKİR 2: Perspektif tilt — [data-tilt] kartlar ========================
  // Saf CSS 3D transform. [data-tilt-lift] çocuk öğe karttan "yükselir" (translateZ).
  function initTilt() {
    var cards = document.querySelectorAll('[data-tilt]');
    Array.prototype.forEach.call(cards, function (card) {
      var lift = card.querySelector('[data-tilt-lift]');
      card.style.transition = 'transform .12s ease, box-shadow .2s ease, border-color .2s ease';
      card.style.transformStyle = 'preserve-3d';
      if (lift) lift.style.transition = 'transform .18s ease';
      card.addEventListener('pointermove', function (e) {
        if (REDUCED) return;
        var b = card.getBoundingClientRect();
        var px = (e.clientX - b.left) / b.width - 0.5;
        var py = (e.clientY - b.top) / b.height - 0.5;
        card.style.transform = 'perspective(900px) rotateY(' + (px * 9) + 'deg) rotateX(' + (-py * 9) + 'deg) translateZ(8px)';
        card.style.boxShadow = '0 24px 60px rgba(0,0,0,.5)';
        card.style.borderColor = 'rgba(56,189,248,.5)';
        if (lift) lift.style.transform = 'translateZ(36px)';
      }, { passive: true });
      card.addEventListener('pointerleave', function () {
        card.style.transform = '';
        card.style.boxShadow = '';
        card.style.borderColor = '';
        if (lift) lift.style.transform = '';
      });
    });
  }
})();
