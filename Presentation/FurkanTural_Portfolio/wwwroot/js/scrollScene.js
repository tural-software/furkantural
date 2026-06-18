// =============================================================================
// scrollScene.js
// Scroll-driven Three.js sahne katmanı: tüm-sayfa sabit, etkileşimsiz tek canvas
// (#scroll-scene, z-index:0). Her "ekran" (section) için bir mesh tanımlı; sayfada
// kaydırdıkça aktif mesh idle döner/nefes alır, komşuya geçişte scale+opacity ile
// crossfade olur (kamera sabit, mesh'ler transform olur). GSAP/ScrollTrigger YOK —
// scrollProgress = scrollY / innerHeight her frame elle okunur, easing elle yazılır.
//
// background-three.js / portfolio-3d.js ile AYNI guard deseni:
//   - prefers-reduced-motion → sahne hiç başlamaz (statik içerik korunur)
//   - WebGL yoksa / modül yüklenmezse → sessizce çık
//   - sekme gizliyken rAF duraklatılır (pil/CPU)
// three.js aynı pinned CDN URL'inden yüklenir (tek fetch, paylaşılan modül).
// =============================================================================
(function () {
  'use strict';

  // 1) Hareket azaltma → sahneyi hiç kurma (a11y; içerik statik kalır).
  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  // 2) WebGL desteğini ayrı probe canvas'ta test et (asıl context'i kirletme).
  function webglSupported() {
    try {
      var p = document.createElement('canvas');
      return !!(window.WebGLRenderingContext && (p.getContext('webgl') || p.getContext('experimental-webgl')));
    } catch (e) { return false; }
  }
  if (!webglSupported()) return;

  // 3) Canvas'ı dinamik oluştur + body'ye ekle (Razor/_Layout'a dokunmadan).
  //    Stil inline → site.css'e bağımlılık yok; arka plan transparent (alpha).
  var canvas = document.createElement('canvas');
  canvas.id = 'scroll-scene';
  canvas.setAttribute('aria-hidden', 'true');
  var s = canvas.style;
  s.position = 'fixed'; s.top = '0'; s.left = '0';
  s.width = '100%'; s.height = '100%';
  s.zIndex = '0'; s.pointerEvents = 'none';
  s.opacity = '0'; s.transition = 'opacity 1.2s ease';
  document.body.appendChild(canvas);

  var THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.min.js';
  import(THREE_URL)
    .then(function (THREE) { try { init(THREE); } catch (e) { canvas.remove(); } })
    .catch(function () { canvas.remove(); /* yükleme başarısız → statik içerik korunur */ });

  // --- Sahne config: her ekran için bir mesh + renk (site paletiyle hizalı) -----
  // mesh tipleri: torusKnot, icosa, cubeGrid, orbits, wavePlane, helix, octaRing.
  // Sıralama Index.cshtml section sırasıyla eşlenir; ekran sayısı config'i aşarsa
  // modulo ile döngüye girer (sayfa uzunluğundan bağımsız sağlam).
  var CONFIGS = [
    { mesh: 'torusKnot', color: 0x38bdf8 }, // 0 hero    — sky
    { mesh: 'icosa',     color: 0x7dd3fc }, // 1 about   — açık sky
    { mesh: 'cubeGrid',  color: 0x4f46e5 }, // 2 skills  — indigo
    { mesh: 'orbits',    color: 0xa78bfa }, // 3 exp/edu — mor
    { mesh: 'wavePlane', color: 0x38bdf8 }, // 4 projects— sky
    { mesh: 'helix',     color: 0x8b5cf6 }, // 5 music   — menekşe
    { mesh: 'octaRing',  color: 0x7dd3fc }, // 6 services— açık sky
    { mesh: 'torusKnot', color: 0x4f46e5 }  // 7 contact — indigo
  ];

  var OPACITY = 0.6; // wireframe mesh'ler içeriğin arkasında — zarif, baskın değil.

  // --- Elle easing (harici kütüphane yok) --------------------------------------
  function easeInOutCubic(x) {
    return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
  }

  function init(THREE) {
    var renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));

    var scene = new THREE.Scene();
    var cam = new THREE.PerspectiveCamera(45, 1, 0.1, 100);
    cam.position.z = 6.2; // sabit — mesh'ler transform olur, kamera değil.

    // --- mesh kurucular: her biri { obj, mats[], spinX, spinY, wave? } döndürür -
    function wireMat(color) {
      return new THREE.MeshBasicMaterial({ color: color, wireframe: true, transparent: true, opacity: 0 });
    }

    function buildTorusKnot(color) {
      var m = wireMat(color);
      var o = new THREE.Mesh(new THREE.TorusKnotGeometry(1.05, 0.32, 110, 16), m);
      return { obj: o, mats: [m], spinX: 0.0016, spinY: 0.0042 };
    }
    function buildIcosa(color) {
      var m = wireMat(color);
      var o = new THREE.Mesh(new THREE.IcosahedronGeometry(1.5, 1), m);
      return { obj: o, mats: [m], spinX: 0.0024, spinY: 0.0036 };
    }
    function buildCubeGrid(color) {
      var m = wireMat(color);
      var g = new THREE.Group(), geo = new THREE.BoxGeometry(0.34, 0.34, 0.34);
      for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++) for (var z = -1; z <= 1; z++) {
        var b = new THREE.Mesh(geo, m);
        b.position.set(x * 0.82, y * 0.82, z * 0.82);
        g.add(b);
      }
      return { obj: g, mats: [m], spinX: 0.0030, spinY: 0.0040 };
    }
    function buildOrbits(color) {
      var m = wireMat(color);
      var g = new THREE.Group();
      g.add(new THREE.Mesh(new THREE.IcosahedronGeometry(0.5, 0), m));
      var radii = [1.0, 1.4, 1.8], rots = [[0, 0, 0], [1.1, 0.4, 0], [0.5, 1.2, 0.3]];
      for (var i = 0; i < radii.length; i++) {
        var ring = new THREE.Mesh(new THREE.TorusGeometry(radii[i], 0.02, 8, 80), m);
        ring.rotation.set(rots[i][0], rots[i][1], rots[i][2]);
        g.add(ring);
      }
      return { obj: g, mats: [m], spinX: 0.0010, spinY: 0.0050 };
    }
    function buildWavePlane(color) {
      var m = wireMat(color);
      var geo = new THREE.PlaneGeometry(3.8, 3.8, 22, 22);
      var o = new THREE.Mesh(geo, m);
      o.rotation.x = -1.0; // hafif yatık → dalga görünür
      var pos = geo.attributes.position, base = pos.array.slice();
      function wave(t) {
        for (var k = 0; k < pos.count; k++) {
          var ix = k * 3, bx = base[ix], by = base[ix + 1];
          pos.array[ix + 2] = Math.sin(bx * 1.4 + t * 1.6) * 0.32 + Math.cos(by * 1.2 + t * 1.1) * 0.28;
        }
        pos.needsUpdate = true;
      }
      return { obj: o, mats: [m], spinX: 0, spinY: 0.0026, wave: wave };
    }
    function buildHelix(color) {
      var m = new THREE.LineBasicMaterial({ color: color, transparent: true, opacity: 0 });
      var pts = [];
      for (var i = 0; i <= 220; i++) {
        var a = i * 0.28;
        pts.push(new THREE.Vector3(Math.cos(a) * 0.95, i * 0.013 - 1.45, Math.sin(a) * 0.95));
      }
      var geo = new THREE.BufferGeometry().setFromPoints(pts);
      var o = new THREE.Line(geo, m);
      return { obj: o, mats: [m], spinX: 0.0008, spinY: 0.0052 };
    }
    function buildOctaRing(color) {
      var m = wireMat(color);
      var g = new THREE.Group(), geo = new THREE.OctahedronGeometry(0.42, 0), N = 7;
      for (var i = 0; i < N; i++) {
        var a = (i / N) * Math.PI * 2, oc = new THREE.Mesh(geo, m);
        oc.position.set(Math.cos(a) * 1.35, Math.sin(a) * 1.35, 0);
        g.add(oc);
      }
      return { obj: g, mats: [m], spinX: 0.0018, spinY: 0.0044 };
    }

    var BUILDERS = {
      torusKnot: buildTorusKnot, icosa: buildIcosa, cubeGrid: buildCubeGrid,
      orbits: buildOrbits, wavePlane: buildWavePlane, helix: buildHelix, octaRing: buildOctaRing
    };

    // Tüm mesh'leri bir kez kur (hepsi origin'de üst üste; görünürlük opacity ile).
    var meshes = [];
    for (var ci = 0; ci < CONFIGS.length; ci++) {
      var cfg = CONFIGS[ci], built = BUILDERS[cfg.mesh](cfg.color);
      built.vis = 0;                  // mevcut görünürlük (0..1), her frame lerp'lenir
      built.obj.visible = false;
      built.obj.scale.setScalar(0.001);
      scene.add(built.obj);
      meshes.push(built);
    }
    var LEN = meshes.length;

    function resize() {
      var w = window.innerWidth, h = window.innerHeight;
      if (!w || !h) return;
      renderer.setSize(w, h, false);
      cam.aspect = w / h; cam.updateProjectionMatrix();
    }
    window.addEventListener('resize', resize, { passive: true });
    resize();

    var raf = null, t0 = performance.now();

    function frame(now) {
      var t = (now - t0) * 0.001;

      // scrollProgress → aktif ekran + geçiş yüzdesi (spec'teki formül).
      var sp = window.scrollY / window.innerHeight;
      if (sp < 0) sp = 0;
      var idx = Math.floor(sp);
      var tr = sp - idx;                  // 0.0 → 1.0
      var f = easeInOutCubic(tr);

      // Hedef görünürlük: aktif mesh 1→0 boşalır, sonraki 0→1 dolar (crossfade).
      var act = idx % LEN, nxt = (idx + 1) % LEN;
      for (var i = 0; i < LEN; i++) {
        var target = 0;
        if (i === act) target += (1 - f);
        if (i === nxt) target += f;
        if (target > 1) target = 1;

        var m = meshes[i];
        m.vis += (target - m.vis) * 0.12;          // yumuşak lerp (hızlı scroll'da bile)
        var v = m.vis;
        var on = v > 0.003;
        m.obj.visible = on;
        if (!on) continue;

        // idle: her görünür mesh döner; aktif olan hafif "nefes alır".
        m.obj.rotation.x += m.spinX;
        m.obj.rotation.y += m.spinY;
        if (m.wave) m.wave(t);
        var breathe = 1 + Math.sin(t * 1.5) * 0.03 * v;
        m.obj.scale.setScalar((0.2 + 0.8 * v) * breathe);
        for (var j = 0; j < m.mats.length; j++) m.mats[j].opacity = OPACITY * v;
      }

      renderer.render(scene, cam);
      raf = requestAnimationFrame(frame);
    }

    function start() { if (raf === null) raf = requestAnimationFrame(frame); }
    function stop() { if (raf !== null) { cancelAnimationFrame(raf); raf = null; } }

    // Sekme gizliyken duraklat (pil/CPU). Sahne tüm-sayfa sabit → IO gerekmez.
    document.addEventListener('visibilitychange', function () {
      if (document.hidden) stop(); else start();
    });

    canvas.style.opacity = '1';
    start();
  }
})();
