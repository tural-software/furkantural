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

    // Parçacık sayısı ekran genişliğine göre (mobilde düşük tut). Yoğunluk artırıldı
    // + dağılım hacmi büyütüldü (daha çok nokta, tıkanmadan).
    var count = window.innerWidth < 768 ? 360 : 900;
    var positions = new Float32Array(count * 3);
    var pcolors = new Float32Array(count * 3);   // her parçacığa kendi rengi (çok-renkli)
    var pc = new THREE.Color();
    for (var i = 0; i < count; i++) {
      positions[i * 3]     = (Math.random() - 0.5) * 820;
      positions[i * 3 + 1] = (Math.random() - 0.5) * 520;
      positions[i * 3 + 2] = (Math.random() - 0.5) * 520;
      // Renk çarkı boyunca rastgele canlı ton (yüksek doygunluk → koyu fonda okunur).
      pc.setHSL(Math.random(), 0.65 + Math.random() * 0.3, 0.52 + Math.random() * 0.16);
      pcolors[i * 3]     = pc.r;
      pcolors[i * 3 + 1] = pc.g;
      pcolors[i * 3 + 2] = pc.b;
    }
    var geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('aColor', new THREE.BufferAttribute(pcolors, 3));

    // Animasyon B: cursor-reaktif parçacıklar — imlece yakın noktalar parlar + büyür.
    // Konumlar STATİK kalır → constellation çizgileri geçerli (statik-çizgi optimizasyonu korunur).
    // Renk artık per-parçacık (aColor); imlece yakın noktalar beyaza doğru parlar.
    var pointsUniforms = {
      u_size:  { value: 3.6 },
      u_mouse: { value: new THREE.Vector2(99, 99) }   // başlangıçta uzak → parlama yok
    };
    var material = new THREE.ShaderMaterial({
      uniforms: pointsUniforms,
      transparent: true,
      depthWrite: false,
      vertexShader: [
        'attribute vec3 aColor;',
        'uniform float u_size; uniform vec2 u_mouse;',
        'varying float vGlow;',
        'varying vec3 vColor;',
        'void main(){',
        '  vColor = aColor;',
        '  vec4 mv = modelViewMatrix * vec4(position, 1.0);',
        '  gl_Position = projectionMatrix * mv;',
        '  vec2 ndc = gl_Position.xy / gl_Position.w;',
        '  float glow = 1.0 - smoothstep(0.0, 0.45, distance(ndc, u_mouse));',
        '  vGlow = glow;',
        '  gl_PointSize = u_size * (300.0 / -mv.z) * (1.0 + glow * 1.8);',
        '}'
      ].join('\n'),
      fragmentShader: [
        'varying float vGlow;',
        'varying vec3 vColor;',
        'void main(){',
        '  float dd = distance(gl_PointCoord, vec2(0.5));',
        '  if (dd > 0.5) discard;',
        '  float soft = smoothstep(0.5, 0.0, dd);',
        '  vec3 col = mix(vColor, vec3(1.0), vGlow * 0.5);',  // imlece yakın → beyaza doğru parla
        '  float alpha = soft * (0.9 + vGlow * 0.1);',         // belirgin → neredeyse tam opak
        '  gl_FragColor = vec4(col, alpha);',
        '}'
      ].join('\n')
    });
    var points = new THREE.Points(geometry, material);

    // Parçacıkları bir grupta yavaşça döndür. (Constellation bağlantı çizgileri
    // kaldırıldı — yalnız serbest parçacıklar.)
    var group = new THREE.Group();
    group.add(points);
    scene.add(group);

    // --- Arka planda "ay benzeri" uydular — merkez (origin) etrafında yörünge ---
    // Her uydu FARKLI uzaklıkta (orbitR) + farklı eğim/düğüm/hızda dairesel yörüngede
    // döner (görünür Dünya yok; merkez sahnenin odağı). Yumuşak yönlü ışık + ay dokusu
    // → gündüz/gece terminatörü.
    scene.add(new THREE.AmbientLight(0x8090b0, 0.55));
    var sun = new THREE.DirectionalLight(0xffffff, 1.15);
    sun.position.set(-0.6, 0.85, 1.0);
    scene.add(sun);

    // Kullanıcının sağladığı uydu dokuları (wwwroot/img). HER variant görseli için BİR
    // uydu (1:1). LoadingManager: hepsi yüklenmeden hiçbiri görünmez → yüklenince fade-in
    // (bir anda çıkmaz). Boşluk %20 ile encode'lu.
    var SAT_URLS = [
      '/img/globe-satellite%20(1).webp', '/img/globe-satellite%20(2).webp',
      '/img/globe-satellite%20(3).webp', '/img/globe-satellite%20(4).webp'
    ];
    var satManager = new THREE.LoadingManager();
    var satLoader = new THREE.TextureLoader(satManager);

    var sats = [];
    var satReady = false, satFade = 0;
    var SAT_N = SAT_URLS.length;
    for (var s = 0; s < SAT_N; s++) {
      var tex = satLoader.load(SAT_URLS[s]); tex.colorSpace = THREE.SRGBColorSpace;
      var rad = 9 + Math.random() * 13;
      var sat = new THREE.Mesh(
        new THREE.SphereGeometry(rad, 28, 28),
        new THREE.MeshStandardMaterial({
          map: tex, roughness: 1.0, metalness: 0.0,
          emissive: 0x0a0e18, emissiveMap: tex, emissiveIntensity: 0.4,
          transparent: true, opacity: 0          // fade-in için saydam başlar
        })
      );
      sat.visible = false;                         // tüm dokular yüklenene kadar gizli
      sat.rotation.set(Math.random() * 6.28, Math.random() * 6.28, 0);
      // Yörünge parametreleri: her uydu farklı uzaklık (orbitR) + farklı düzlem (inc/rot) + hız.
      var orbitR = 85 + s * 55;                    // farklı uzaklıklar: 85,140,195,250
      sat.userData = {
        orbitR: orbitR,
        ang: Math.random() * Math.PI * 2,                              // başlangıç açısı
        spd: 0.0052 * Math.sqrt(85 / orbitR),                          // dış yörünge daha yavaş
        inc: (s - (SAT_N - 1) / 2) * 0.5 + (Math.random() - 0.5) * 0.2, // yörünge eğimi
        rot: Math.random() * Math.PI * 2,                              // yörünge düzlemi Y-dönüşü
        rx: (Math.random() - 0.5) * 0.004, ry: 0.002 + Math.random() * 0.004
      };
      scene.add(sat);
      sats.push(sat);
    }
    // Tüm uydu dokuları yüklendiğinde hepsini birden görünür yap → frame'de fade-in başlar.
    satManager.onLoad = function () {
      satReady = true;
      for (var k = 0; k < sats.length; k++) sats[k].visible = true;
    };

    // --- Animasyon A: shader aurora katmanı (parçacıkların ARKASINDA) ---
    // Tek fullscreen quad; vertex matrisi yok-sayar (doğrudan NDC) → daima tam ekran.
    // Perf için yalnız geniş ekranlarda; mobil/zayıf GPU'da atlanır (parçacıklar kalır).
    var aurora = null;
    if (window.innerWidth >= 768) {
      var auroraUniforms = {
        u_time: { value: 0 },
        u_res:  { value: new THREE.Vector2(1, 1) },
        u_c1:   { value: new THREE.Color(0x38bdf8) },
        u_c2:   { value: new THREE.Color(0x4f46e5) }
      };
      var auroraMat = new THREE.ShaderMaterial({
        uniforms: auroraUniforms,
        transparent: true,
        depthTest: false,
        depthWrite: false,
        vertexShader:
          'varying vec2 vUv; void main(){ vUv = uv; gl_Position = vec4(position.xy, 0.0, 1.0); }',
        fragmentShader: [
          'varying vec2 vUv;',
          'uniform float u_time; uniform vec2 u_res; uniform vec3 u_c1; uniform vec3 u_c2;',
          'float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }',
          'float noise(vec2 p){ vec2 i = floor(p), f = fract(p);',
          '  float a = hash(i), b = hash(i + vec2(1.,0.)), c = hash(i + vec2(0.,1.)), d = hash(i + vec2(1.,1.));',
          '  vec2 u = f * f * (3. - 2. * f); return mix(mix(a,b,u.x), mix(c,d,u.x), u.y); }',
          'float fbm(vec2 p){ float v = 0.0, a = 0.5; for (int i = 0; i < 4; i++){ v += a * noise(p); p *= 2.0; a *= 0.5; } return v; }',
          'void main(){',
          '  vec2 uv = vUv; uv.x *= u_res.x / max(u_res.y, 1.0);',
          '  float t = u_time * 0.03;',
          '  vec2 q = vec2(fbm(uv * 1.5 + t), fbm(uv * 1.5 - t + 3.0));',
          '  float f = fbm(uv * 2.0 + q * 1.4 + vec2(t * 0.4, -t * 0.4));',
          '  vec3 col = mix(u_c1, u_c2, smoothstep(0.2, 0.85, f));',
          '  float alpha = smoothstep(0.4, 0.95, f) * 0.32;',  // çok düşük → zarif
          '  gl_FragColor = vec4(col, alpha);',
          '}'
        ].join('\n')
      });
      var auroraMesh = new THREE.Mesh(new THREE.PlaneGeometry(2, 2), auroraMat);
      auroraMesh.frustumCulled = false;
      var auroraScene = new THREE.Scene();
      auroraScene.add(auroraMesh);
      aurora = { scene: auroraScene, cam: new THREE.Camera(), uniforms: auroraUniforms };
    }

    var raf = null;
    var mouseX = 0, mouseY = 0;
    var t0 = performance.now();

    // FİKİR 3: scroll'a bağlı kamera dolly — sayfa ilerleme oranını izle.
    var scrollProg = 0;
    function onScroll() {
      var h = document.documentElement;
      var max = h.scrollHeight - h.clientHeight;
      scrollProg = max > 0 ? (window.scrollY || h.scrollTop) / max : 0;
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    function resize() {
      var w = canvas.clientWidth, h = canvas.clientHeight;
      if (!w || !h) return;
      renderer.setSize(w, h, false);
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
      if (aurora) aurora.uniforms.u_res.value.set(w, h);
    }

    function frame(now) {
      var t = (now - t0) * 0.0001;
      group.rotation.y = t + scrollProg * 1.4;          // scroll'la ekstra dönüş
      group.rotation.x = t * 0.4 + scrollProg * 0.3;
      // Hafif fare parallax'ı yumuşatarak uygula.
      camera.position.x += (mouseX * 40 - camera.position.x) * 0.04;
      camera.position.y += (-mouseY * 40 - camera.position.y) * 0.04;
      camera.position.z = 320 - scrollProg * 150;        // aşağı kaydırınca yaklaş (dolly)
      camera.lookAt(scene.position);

      // Uydular: tüm dokular yüklendiyse fade-in + merkez etrafında dairesel yörünge
      // (farklı uzaklık/eğim/hız) + kendi ekseninde dönüş. Yüklenmeden gizli.
      if (satReady) {
        if (satFade < 1) satFade = Math.min(1, satFade + 0.012);   // ~1.4s yumuşak fade-in
        for (var si = 0; si < sats.length; si++) {
          var st = sats[si], u = st.userData;
          u.ang += u.spd;                                    // yörüngede ilerle
          // XZ düzleminde daire → X ekseninde 'inc' kadar eğ → Y ekseninde 'rot' kadar döndür.
          var cx = Math.cos(u.ang) * u.orbitR;
          var cz = Math.sin(u.ang) * u.orbitR;
          var yt = -cz * Math.sin(u.inc);
          var zt = cz * Math.cos(u.inc);
          st.position.set(
            cx * Math.cos(u.rot) - zt * Math.sin(u.rot),
            yt,
            cx * Math.sin(u.rot) + zt * Math.cos(u.rot)
          );
          st.rotation.x += u.rx; st.rotation.y += u.ry;      // kendi ekseninde dönüş
          if (satFade < 1) st.material.opacity = satFade;
        }
      }

      if (aurora) {
        // İki geçiş: önce aurora (arka), sonra parçacıklar (ön) — tek temizleme.
        aurora.uniforms.u_time.value = (now - t0) * 0.001;
        renderer.autoClear = false;
        renderer.clear();
        renderer.render(aurora.scene, aurora.cam);
        renderer.render(scene, camera);
      } else {
        renderer.render(scene, camera);
      }
      raf = requestAnimationFrame(frame);
    }

    function start() { if (raf === null) raf = requestAnimationFrame(frame); }
    function stop()  { if (raf !== null) { cancelAnimationFrame(raf); raf = null; } }

    resize();
    window.addEventListener('resize', resize);
    window.addEventListener('pointermove', function (e) {
      mouseX = (e.clientX / window.innerWidth) - 0.5;
      mouseY = (e.clientY / window.innerHeight) - 0.5;
      // NDC'ye çevir (x: [-1,1], y yukarı pozitif) → cursor-reaktif parlama.
      pointsUniforms.u_mouse.value.set(mouseX * 2.0, -mouseY * 2.0);
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
