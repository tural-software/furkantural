// =============================================================================
// exploreMode.js — Keşif Modu: 3B gezilebilir dünya (üçüncü-şahıs kutu)
//
// Çift-mod: mevcut erişilebilir kaydırmalı site KORUNUR; bu opt-in bir katman.
// Kutuyu bir dünyada sürersin; navbar'daki her bölüm bir "istasyon". İstasyona
// yaklaşınca o bölümün GERÇEK DOM içeriği (.section-container) bir hologram panele
// TAŞINIR (klon değil → dinamik veri, bağlı form/tilt handler'ları, #ftMusic korunur);
// kapatınca yerine geri konur. Navbar'a tıklayınca kutu o istasyona ışınlanır ve
// oyun devam eder.
//
// Guard deseni (background-three.js / scrollScene.js ile aynı):
//   - prefers-reduced-motion → toggle hiç enjekte edilmez (normal site korunur)
//   - WebGL yok / modül yüklenmezse → sessizce çık
//   - sekme gizliyken rAF duraklatılır
// three.js aynı pinned CDN URL'inden yüklenir (paylaşılan modül; lazy — yalnız ilk
// girişte). Keşif aktifken `explore:enter`, çıkışta `explore:exit` event'i yayınlanır
// → diğer 3B sahneler kendini duraklatır (GPU boşalır).
// =============================================================================
(function () {
  'use strict';

  // 1) Hareket azaltma → özelliği hiç sunma (içerik statik, normal site çalışır).
  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

  // 2) WebGL probe (asıl context'i kirletmeden).
  function webglSupported() {
    try {
      var p = document.createElement('canvas');
      return !!(window.WebGLRenderingContext && (p.getContext('webgl') || p.getContext('experimental-webgl')));
    } catch (e) { return false; }
  }
  if (!webglSupported()) return;

  var THREE_URL = 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.min.js';
  var IS_TOUCH = ('ontouchstart' in window) || (navigator.maxTouchPoints || 0) > 0;
  var PALETTE = [0x38bdf8, 0x7dd3fc, 0x4f46e5, 0xa78bfa, 0x38bdf8, 0x8b5cf6, 0x7dd3fc, 0x4f46e5];

  // --- modül durumu -----------------------------------------------------------
  var THREE = null, built = false, building = false, active = false;
  var renderer, scene, camera, player, raf = null;
  var stations = [];            // { el, content, label, idx, pos, group }
  var navLinks = [];            // istasyonla eşleşen .nav-link öğeleri (sıra = istasyon sırası)
  var keys = Object.create(null);
  var yaw = 0;                  // kamera yörünge açısı
  var look = { on: false, id: -1, x: 0 };
  var joy = { on: false, id: -1, fx: 0, fy: 0, el: null, knob: null };
  var panelOpen = null;         // açık istasyon idx'i ya da null
  var justClosed = -1;          // kapanan istasyon — yarıçaptan çıkmadan yeniden açma
  var hintEl = null, toggleBtn = null;
  var UP = null;                // THREE.Vector3(0,1,0) — build'de set

  var CAM_DIST = 9, CAM_HEIGHT = 6, SPEED = 0.26, RING_R = 22;
  var NEAR_OPEN = 6.5, NEAR_LEAVE = 9.5;

  // --- toggle butonu (navbar'a enjekte) --------------------------------------
  document.addEventListener('DOMContentLoaded', function () {
    var actions = document.querySelector('.nav-actions');
    if (!actions) return;

    // İstasyonla eşleşecek navbar linkleri (href → mevcut section id).
    var links = document.querySelectorAll('.nav-links .nav-link');
    Array.prototype.forEach.call(links, function (a) {
      var href = a.getAttribute('href') || '';
      if (href.charAt(0) !== '#') return;
      var sec = document.getElementById(href.slice(1));
      if (sec) navLinks.push(a);
    });
    if (!navLinks.length) return;     // eşleşen bölüm yok → özelliği sunma

    toggleBtn = document.createElement('button');
    toggleBtn.type = 'button';
    toggleBtn.className = 'explore-toggle-btn';
    toggleBtn.setAttribute('aria-pressed', 'false');
    toggleBtn.innerHTML = '<span class="explore-toggle-btn__icon" aria-hidden="true">🧭</span>' +
      '<span class="explore-toggle-btn__text">Keşfet</span>';
    toggleBtn.addEventListener('click', function () { active ? exit() : enter(); });
    actions.insertBefore(toggleBtn, actions.firstChild);

    // Işınlanma: keşif aktifken navbar linkleri kutuyu istasyona taşır (capture).
    navLinks.forEach(function (a, i) {
      a.addEventListener('click', function (e) {
        if (!active) return;            // normal modda olağan anchor davranışı
        e.preventDefault();
        e.stopPropagation();
        teleport(i);
        var nl = document.getElementById('navLinks');
        if (nl) nl.classList.remove('open');   // mobil menüyü kapat (site.js deseni)
      }, true);
    });

    document.addEventListener('keydown', function (e) {
      if (!active) return;
      if (e.key === 'Escape') { if (panelOpen !== null) closePanel(); else exit(); return; }
      keys[(e.key || '').toLowerCase()] = true;
    });
    document.addEventListener('keyup', function (e) { keys[(e.key || '').toLowerCase()] = false; });
    document.addEventListener('visibilitychange', function () {
      if (!active) return;
      if (document.hidden) stopLoop(); else startLoop();
    });
    window.addEventListener('resize', onResize, { passive: true });
  });

  // --- giriş / çıkış ----------------------------------------------------------
  function enter() {
    if (active) return;
    if (!built) { buildWorld(); return; }   // ilk giriş: yüklenince buildWorld enter'ı çağırır
    active = true;
    document.documentElement.classList.add('explore-active');
    if (IS_TOUCH) document.documentElement.classList.add('is-touch');
    renderer.domElement.style.display = 'block';
    requestAnimationFrame(function () { renderer.domElement.classList.add('is-on'); });
    if (toggleBtn) {
      toggleBtn.setAttribute('aria-pressed', 'true');
      toggleBtn.querySelector('.explore-toggle-btn__text').textContent = 'Çıkış';
    }
    resetPlayer();
    onResize();
    showHint();
    window.dispatchEvent(new CustomEvent('explore:enter'));
    startLoop();
  }

  function exit() {
    if (!active) return;
    if (panelOpen !== null) closePanel();
    active = false;
    stopLoop();
    renderer.domElement.classList.remove('is-on');
    setTimeout(function () { if (!active) renderer.domElement.style.display = 'none'; }, 500);
    document.documentElement.classList.remove('explore-active');
    if (toggleBtn) {
      toggleBtn.setAttribute('aria-pressed', 'false');
      toggleBtn.querySelector('.explore-toggle-btn__text').textContent = 'Keşfet';
    }
    window.dispatchEvent(new CustomEvent('explore:exit'));
  }

  function resetPlayer() {
    player.position.set(0, 0, 0);
    yaw = 0;
    keys = Object.create(null);
    joy.fx = joy.fy = 0;
    justClosed = -1;
    snapCamera();
  }

  // --- dünya kurulumu (lazy: ilk girişte) ------------------------------------
  function buildWorld() {
    if (building || built) return;
    building = true;
    import(THREE_URL).then(function (mod) {
      THREE = mod;
      UP = new THREE.Vector3(0, 1, 0);

      var canvas = document.createElement('canvas');
      canvas.id = 'explore-canvas';
      canvas.setAttribute('aria-hidden', 'true');
      canvas.style.display = 'none';
      document.body.appendChild(canvas);

      renderer = new THREE.WebGLRenderer({ canvas: canvas, antialias: true });
      renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
      bindLook();   // bakış girişi (fare/dokunma) — renderer hazır

      var BG = 0x080b12;
      scene = new THREE.Scene();
      scene.background = new THREE.Color(BG);
      scene.fog = new THREE.Fog(BG, 26, 80);

      camera = new THREE.PerspectiveCamera(60, 1, 0.1, 200);

      // Zemin: koyu düzlem + neon grid.
      var floor = new THREE.Mesh(
        new THREE.PlaneGeometry(400, 400),
        new THREE.MeshBasicMaterial({ color: 0x0b1020 })
      );
      floor.rotation.x = -Math.PI / 2; floor.position.y = -0.02; scene.add(floor);
      var grid = new THREE.GridHelper(240, 120, 0x38bdf8, 0x1b2740);
      grid.material.transparent = true; grid.material.opacity = 0.5; scene.add(grid);

      // Oyuncu kutusu: koyu çekirdek + accent kenar teli.
      player = new THREE.Group();
      var box = new THREE.BoxGeometry(1.1, 1.1, 1.1);
      player.add(new THREE.Mesh(box, new THREE.MeshBasicMaterial({ color: 0x0e1424 })));
      player.add(new THREE.LineSegments(
        new THREE.EdgesGeometry(box),
        new THREE.LineBasicMaterial({ color: 0x38bdf8 })
      ));
      player.position.y = 0.7;
      scene.add(player);

      // İstasyonlar — navbar linklerinden (etiket = link metni, hedef = href).
      navLinks.forEach(function (a, i) {
        var sec = document.getElementById(a.getAttribute('href').slice(1));
        var ang = (i / navLinks.length) * Math.PI * 2;
        var pos = new THREE.Vector3(Math.cos(ang) * RING_R, 0, Math.sin(ang) * RING_R);
        var color = PALETTE[i % PALETTE.length];

        var g = new THREE.Group(); g.position.copy(pos);
        var mono = new THREE.BoxGeometry(1.6, 4.2, 1.6);
        var monoMesh = new THREE.Mesh(mono, new THREE.MeshBasicMaterial({ color: 0x0e1424 }));
        monoMesh.position.y = 2.1; g.add(monoMesh);
        var edge = new THREE.LineSegments(new THREE.EdgesGeometry(mono),
          new THREE.LineBasicMaterial({ color: color }));
        edge.position.y = 2.1; g.add(edge);
        // Taban halkası (zeminde parlak iz).
        var ring = new THREE.Mesh(new THREE.RingGeometry(2.4, 2.7, 40),
          new THREE.MeshBasicMaterial({ color: color, transparent: true, opacity: 0.5, side: THREE.DoubleSide }));
        ring.rotation.x = -Math.PI / 2; ring.position.y = 0.02; g.add(ring);
        // Etiket (billboard sprite).
        var label = makeLabel((a.textContent || '').trim(), color);
        label.position.y = 5.4; g.add(label);
        scene.add(g);

        stations.push({
          el: sec,
          content: sec.querySelector('.section-container') || sec,
          label: (a.textContent || '').trim(),
          idx: i, pos: pos, group: g
        });
      });

      buildPanel();
      if (IS_TOUCH) buildJoystick();

      built = true; building = false;
      enter();   // yükleme bitti → moda gir
    }).catch(function () { building = false; /* yükleme başarısız → normal site kalır */ });
  }

  function makeLabel(text, color) {
    var cv = document.createElement('canvas'); cv.width = 512; cv.height = 128;
    var ctx = cv.getContext('2d');
    var hex = '#' + ('000000' + color.toString(16)).slice(-6);
    ctx.font = 'bold 56px Inter, system-ui, sans-serif';
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.shadowColor = hex; ctx.shadowBlur = 22; ctx.fillStyle = hex;
    ctx.fillText(text, 256, 64);
    var tex = new THREE.CanvasTexture(cv);
    tex.colorSpace = THREE.SRGBColorSpace;
    var sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, transparent: true, depthWrite: false }));
    sp.scale.set(6.4, 1.6, 1);
    return sp;
  }

  // --- kamera -----------------------------------------------------------------
  function camTarget(out) {
    return out.set(
      player.position.x + Math.sin(yaw) * CAM_DIST,
      player.position.y + CAM_HEIGHT,
      player.position.z + Math.cos(yaw) * CAM_DIST
    );
  }
  function snapCamera() {
    var t = camTarget(new THREE.Vector3());
    camera.position.copy(t);
    camera.lookAt(player.position.x, player.position.y + 1.0, player.position.z);
    camera.updateMatrixWorld();   // getWorldDirection ilk frame'de güncel olsun
  }

  // --- girdi (fare/dokunma bakış) ---------------------------------------------
  function bindLook() {
    var c = renderer.domElement;
    c.addEventListener('pointerdown', function (e) {
      if (!active || panelOpen !== null) return;
      look.on = true; look.id = e.pointerId; look.x = e.clientX;
      try { c.setPointerCapture(e.pointerId); } catch (ex) {}
    });
    c.addEventListener('pointermove', function (e) {
      if (!look.on || e.pointerId !== look.id) return;
      yaw -= (e.clientX - look.x) * 0.006;
      look.x = e.clientX;
    });
    function end(e) { if (look.on && e.pointerId === look.id) { look.on = false; look.id = -1; } }
    c.addEventListener('pointerup', end);
    c.addEventListener('pointercancel', end);
  }

  // --- mobil joystick ---------------------------------------------------------
  function buildJoystick() {
    var base = document.createElement('div'); base.className = 'explore-joystick';
    var knob = document.createElement('div'); knob.className = 'explore-joystick__knob';
    base.appendChild(knob); document.body.appendChild(base);
    joy.el = base; joy.knob = knob;
    var R = 44;
    function setKnob(dx, dy) { knob.style.transform = 'translate(' + dx + 'px,' + dy + 'px)'; }
    base.addEventListener('pointerdown', function (e) {
      joy.on = true; joy.id = e.pointerId;
      try { base.setPointerCapture(e.pointerId); } catch (ex) {}
      move(e);
    });
    base.addEventListener('pointermove', function (e) { if (joy.on && e.pointerId === joy.id) move(e); });
    function move(e) {
      var b = base.getBoundingClientRect();
      var dx = e.clientX - (b.left + b.width / 2);
      var dy = e.clientY - (b.top + b.height / 2);
      var d = Math.hypot(dx, dy) || 1;
      if (d > R) { dx = dx / d * R; dy = dy / d * R; }
      setKnob(dx, dy);
      joy.fx = dx / R;          // sağ = +strafe
      joy.fy = -dy / R;         // yukarı = +forward
    }
    function end(e) {
      if (!joy.on || e.pointerId !== joy.id) return;
      joy.on = false; joy.id = -1; joy.fx = joy.fy = 0; setKnob(0, 0);
    }
    base.addEventListener('pointerup', end);
    base.addEventListener('pointercancel', end);
  }

  // --- hologram panel (section içeriği TAŞINIR) -------------------------------
  var panelEl, panelTitle, panelBody, placeholder = null;
  function buildPanel() {
    panelEl = document.createElement('div');
    panelEl.className = 'explore-panel';
    panelEl.setAttribute('role', 'dialog');
    panelEl.setAttribute('aria-modal', 'true');
    panelEl.innerHTML =
      '<div class="explore-panel__head">' +
        '<span class="explore-panel__title"></span>' +
        '<button type="button" class="explore-panel__close" aria-label="Kapat">✕</button>' +
      '</div>' +
      '<div class="explore-panel__body"></div>';
    document.body.appendChild(panelEl);
    panelTitle = panelEl.querySelector('.explore-panel__title');
    panelBody = panelEl.querySelector('.explore-panel__body');
    panelEl.querySelector('.explore-panel__close').addEventListener('click', closePanel);
  }

  function openPanel(i) {
    if (panelOpen !== null) closePanel();
    var st = stations[i];
    placeholder = document.createComment('explore-station-' + i);
    st.content.parentNode.insertBefore(placeholder, st.content);
    panelBody.appendChild(st.content);          // GERÇEK düğüm taşınır (klon değil)
    panelTitle.textContent = st.label;
    panelEl.classList.add('is-open');
    panelOpen = i;
  }

  function closePanel() {
    if (panelOpen === null) return;
    var st = stations[panelOpen];
    if (placeholder && placeholder.parentNode) {
      placeholder.parentNode.insertBefore(st.content, placeholder);
      placeholder.parentNode.removeChild(placeholder);
    }
    placeholder = null;
    panelEl.classList.remove('is-open');
    justClosed = panelOpen;     // yarıçaptan çıkana kadar tekrar açma
    panelOpen = null;
  }

  // --- ışınlanma --------------------------------------------------------------
  function teleport(i) {
    var st = stations[i];
    // İstasyonun önünde (merkeze 6 birim çekilmiş) dur → proximity yarıçapında.
    var dir = st.pos.clone().normalize();
    player.position.set(st.pos.x - dir.x * 6, 0, st.pos.z - dir.z * 6);
    snapCamera();
    justClosed = -1;
    openPanel(i);
  }

  // --- döngü ------------------------------------------------------------------
  function frame() {
    // Hareket (panel kapalıyken).
    if (panelOpen === null) {
      var fwd = (keys['w'] || keys['arrowup'] ? 1 : 0) - (keys['s'] || keys['arrowdown'] ? 1 : 0) + joy.fy;
      var stf = (keys['d'] || keys['arrowright'] ? 1 : 0) - (keys['a'] || keys['arrowleft'] ? 1 : 0) + joy.fx;
      if (fwd || stf) {
        var dir = new THREE.Vector3(); camera.getWorldDirection(dir); dir.y = 0; dir.normalize();
        var right = new THREE.Vector3().crossVectors(UP, dir).normalize();
        var mv = new THREE.Vector3()
          .addScaledVector(dir, fwd).addScaledVector(right, stf);
        if (mv.lengthSq() > 0) {
          mv.normalize().multiplyScalar(SPEED);
          player.position.add(mv);
          player.rotation.y = Math.atan2(mv.x, mv.z);
        }
      }
      checkProximity();
    }

    // Kutu hafif "nefes" + kamera takip.
    player.position.y = 0.7 + Math.sin(performance.now() * 0.003) * 0.05;
    var t = camTarget(new THREE.Vector3());
    camera.position.lerp(t, 0.12);
    camera.lookAt(player.position.x, player.position.y + 1.0, player.position.z);

    renderer.render(scene, camera);
    raf = requestAnimationFrame(frame);
  }

  function checkProximity() {
    var ni = -1, nd = 1e9;
    for (var i = 0; i < stations.length; i++) {
      var dx = player.position.x - stations[i].pos.x;
      var dz = player.position.z - stations[i].pos.z;
      var d = Math.sqrt(dx * dx + dz * dz);
      if (d < nd) { nd = d; ni = i; }
    }
    if (nd > NEAR_LEAVE) justClosed = -1;          // tüm yarıçaplardan çıktı
    if (nd < NEAR_OPEN && ni !== justClosed) openPanel(ni);
  }

  function startLoop() { if (raf === null && active) raf = requestAnimationFrame(frame); }
  function stopLoop() { if (raf !== null) { cancelAnimationFrame(raf); raf = null; } }

  function onResize() {
    if (!renderer) return;
    var w = window.innerWidth, h = window.innerHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / h; camera.updateProjectionMatrix();
  }

  function showHint() {
    if (!hintEl) {
      hintEl = document.createElement('div');
      hintEl.className = 'explore-hud explore-hint';
      document.body.appendChild(hintEl);
    }
    hintEl.textContent = IS_TOUCH
      ? 'Joystick ile gez · sürükleyerek bak · istasyona yaklaş · navbar’dan ışınlan'
      : 'WASD ile gez · sürükle-bak · istasyona yaklaş · navbar’dan ışınlan · ESC çıkış';
    hintEl.classList.remove('is-fading');
    hintEl.style.display = 'block';
    clearTimeout(showHint._t);
    showHint._t = setTimeout(function () { hintEl.classList.add('is-fading'); }, 5200);
  }
})();
