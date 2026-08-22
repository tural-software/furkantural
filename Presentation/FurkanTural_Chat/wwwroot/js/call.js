(function () {
    'use strict';

    // chat.js köprüsü (aynı SignalR bağlantısı) hazır olana kadar bekle.
    function boot() {
        var B = window.ChatBridge;
        if (!B || !B.connection) { return setTimeout(boot, 100); }
        if (!window.RTCPeerConnection || !navigator.mediaDevices) { return; } // tarayıcı desteklemiyor

        var conn = B.connection;
        var toast = B.toast || function () {};

        var pc = null, localStream = null, remoteStream = null;
        var audioSender = null, videoSender = null;
        var callId = null, peerId = null, isCaller = false, callType = 'audio';
        var incoming = null;                 // gelen arama verisi
        var pendingCandidates = [];          // remoteDescription öncesi gelen adaylar
        var ringTimeout = null;
        var videoPolicy = null;              // sunucudan gelen efektif video politikası
        var relayOnly = true;                // true → iceTransportPolicy:'relay' (IP gizli); false → 'all' (P2P)
        var audioCtx = null, ringTimer = null, ringNodes = []; // sentezlenmiş zil sesi (Web Audio)
        var minimized = false;               // PiP (köşeye küçültülmüş) durumu
        var lastMinPos = null;               // son PiP konumu (oturum içi)
        var vadRAF = null, vadSource = null, vadAnalyser = null; // konuşma göstergesi (ses aktivitesi)

        var ui = buildUi();
        document.body.appendChild(ui.overlay);

        function buildUi() {
            var overlay = document.createElement('div');
            overlay.className = 'call-overlay';
            overlay.hidden = true;
            overlay.innerHTML =
                '<div class="call-box">' +
                  '<button type="button" class="call-min" title="Küçült" aria-label="Küçült">⤡</button>' +
                  '<video class="call-remote" autoplay playsinline></video>' +
                  '<div class="call-poster"><div class="call-avatar"></div></div>' +
                  '<video class="call-local" autoplay playsinline muted></video>' +
                  '<div class="call-head"><div class="call-name"></div><div class="call-state"></div></div>' +
                  '<div class="call-actions">' +
                    '<button type="button" class="call-btn call-mute" title="Sustur">🎙️</button>' +
                    '<button type="button" class="call-btn call-cam" title="Kamera">🎥</button>' +
                    '<button type="button" class="call-btn call-cog" title="Ses/cihaz ayarları">⚙️</button>' +
                    '<button type="button" class="call-btn call-accept" title="Kabul et">📞</button>' +
                    '<button type="button" class="call-btn call-hang" title="Kapat">📵</button>' +
                  '</div>' +
                  '<div class="call-mini">' +
                    '<button type="button" class="call-mbtn call-expand" title="Büyüt" aria-label="Büyüt">⤢</button>' +
                    '<button type="button" class="call-mbtn call-mmute" title="Sustur" aria-label="Sustur">🎙️</button>' +
                    '<button type="button" class="call-mbtn call-mhang" title="Kapat" aria-label="Kapat">📵</button>' +
                  '</div>' +
                '</div>';
            return {
                overlay: overlay,
                box: overlay.querySelector('.call-box'),
                remote: overlay.querySelector('.call-remote'),
                local: overlay.querySelector('.call-local'),
                poster: overlay.querySelector('.call-poster'),
                avatar: overlay.querySelector('.call-avatar'),
                name: overlay.querySelector('.call-name'),
                state: overlay.querySelector('.call-state'),
                mute: overlay.querySelector('.call-mute'),
                cam: overlay.querySelector('.call-cam'),
                cog: overlay.querySelector('.call-cog'),
                accept: overlay.querySelector('.call-accept'),
                hang: overlay.querySelector('.call-hang'),
                min: overlay.querySelector('.call-min'),
                expand: overlay.querySelector('.call-expand'),
                mmute: overlay.querySelector('.call-mmute'),
                mhang: overlay.querySelector('.call-mhang')
            };
        }

        function peerLabel(id) {
            var f = B.friend(id);
            return f ? (f.displayName || f.username || 'Kullanıcı') : 'Kullanıcı';
        }
        function setAvatar(id) {
            var f = B.friend(id);
            if (f && f.avatarUrl) {
                ui.avatar.style.backgroundImage = "url('" + B.mediaUrl(f.avatarUrl) + "')";
                ui.avatar.textContent = '';
            } else {
                ui.avatar.style.backgroundImage = '';
                ui.avatar.textContent = (B.initial ? B.initial(peerLabel(id)) : '?');
            }
        }

        function showOverlay(mode, id, type) {
            // minimized durumu korunur (calling→in-call geçişinde küçük kalsın); yeni arama hideOverlay'de sıfırlanır.
            setAvatar(id);
            ui.name.textContent = peerLabel(id);
            ui.overlay.hidden = false;
            ui.overlay.dataset.mode = mode;          // calling | incoming | in-call
            ui.overlay.dataset.type = type || 'audio';
            // Moda göre butonlar: calling → yalnız kapat; incoming → kabul + kapat; in-call → sustur(+kamera) + ayar + kapat.
            ui.accept.hidden = (mode !== 'incoming');
            ui.mute.hidden = (mode !== 'in-call');
            ui.cam.hidden = (mode !== 'in-call' || type !== 'video');
            ui.cog.hidden = (mode !== 'in-call');
            ui.min.hidden = (mode === 'incoming'); // gelen aramada küçültme yok
            ui.state.textContent =
                mode === 'incoming' ? ((type === 'video' ? 'Görüntülü' : 'Sesli') + ' arıyor…')
              : mode === 'calling' ? 'Çalıyor…'
              : 'Bağlandı';
        }
        function hideOverlay() {
            setMinimized(false);
            ui.overlay.hidden = true;
            ui.remote.srcObject = null;
            ui.local.srcObject = null;
            delete ui.overlay.dataset.mode;
        }
        function setMinimized(on) {
            minimized = !!on;
            if (minimized) {
                ui.overlay.dataset.min = '1';
                positionWidget(); // mutlak konuma al (sağ-alt köşe / son konum)
            } else {
                delete ui.overlay.dataset.min;
                ui.box.style.left = '';
                ui.box.style.top = '';
            }
        }
        function positionWidget() {
            var bw = ui.box.offsetWidth || 320, bh = ui.box.offsetHeight || 210;
            var pos = lastMinPos || { left: window.innerWidth - bw - 16, top: window.innerHeight - bh - 16 };
            placeWidget(pos.left, pos.top);
        }
        function placeWidget(left, top) {
            var bw = ui.box.offsetWidth, bh = ui.box.offsetHeight;
            left = Math.max(4, Math.min(left, window.innerWidth - bw - 4));
            top = Math.max(4, Math.min(top, window.innerHeight - bh - 4));
            ui.box.style.left = left + 'px';
            ui.box.style.top = top + 'px';
            lastMinPos = { left: left, top: top };
        }

        function ensureAudio() {
            try {
                if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                if (audioCtx && audioCtx.state === 'suspended') audioCtx.resume().catch(function () {});
            } catch (e) { audioCtx = null; }
            return audioCtx;
        }
        function playTone(freqs, durMs, vol) {
            var ctx = audioCtx; if (!ctx) return;
            var t = ctx.currentTime, dur = durMs / 1000;
            var gain = ctx.createGain();
            gain.connect(ctx.destination);
            gain.gain.setValueAtTime(0, t);
            gain.gain.linearRampToValueAtTime(vol, t + 0.02);
            gain.gain.setValueAtTime(vol, t + dur - 0.04);
            gain.gain.linearRampToValueAtTime(0, t + dur);
            freqs.forEach(function (f) {
                var osc = ctx.createOscillator();
                osc.type = 'sine'; osc.frequency.value = f;
                osc.connect(gain); osc.start(t); osc.stop(t + dur + 0.02);
                ringNodes.push(osc);
            });
        }
        function startRinging(kind) {
            stopRinging();
            if (!ensureAudio()) return;
            if (kind === 'incoming') {
                var burst = function () {
                    playTone([440, 554], 380, 0.20);
                    setTimeout(function () { if (ringTimer) playTone([440, 554], 380, 0.20); }, 560);
                };
                burst();
                ringTimer = setInterval(burst, 2200);
            } else { // ringback (arayan)
                var rb = function () { playTone([440, 480], 320, 0.10); };
                rb();
                ringTimer = setInterval(rb, 1500);
            }
        }
        function stopRinging() {
            if (ringTimer) { clearInterval(ringTimer); ringTimer = null; }
            ringNodes.forEach(function (n) { try { n.stop(); } catch (e) {} });
            ringNodes = [];
        }

        var prefs = (function () {
            var v = parseFloat(localStorage.getItem('ft.call.volume'));
            return {
                micId: localStorage.getItem('ft.call.micId') || '',
                camId: localStorage.getItem('ft.call.camId') || '',
                speakerId: localStorage.getItem('ft.call.speakerId') || '',
                volume: isNaN(v) ? 1 : Math.min(1, Math.max(0, v))
            };
        })();
        var canSetSink = (typeof ui.remote.setSinkId === 'function');

        function savePrefs() {
            try {
                localStorage.setItem('ft.call.micId', prefs.micId || '');
                localStorage.setItem('ft.call.camId', prefs.camId || '');
                localStorage.setItem('ft.call.speakerId', prefs.speakerId || '');
                localStorage.setItem('ft.call.volume', String(prefs.volume));
            } catch (e) {}
        }
        function audioConstraint() {
            return (prefs.micId && prefs.micId !== 'none') ? { deviceId: { exact: prefs.micId } } : true;
        }
        function videoConstraint() {
            var c = {};
            if (prefs.camId && prefs.camId !== 'none') c.deviceId = { exact: prefs.camId };
            if (videoPolicy && videoPolicy.enabled) {
                c.width = { max: videoPolicy.maxWidth };
                c.height = { max: videoPolicy.maxHeight };
                c.frameRate = { max: videoPolicy.maxFps };
            }
            return Object.keys(c).length ? c : true;
        }
        function mediaErr(kind, e) {
            var dev = (kind === 'audio') ? 'Mikrofon' : 'Kamera';
            var n = e && e.name;
            if (n === 'NotReadableError' || n === 'TrackStartError') return dev + ' başka bir uygulama/sekme tarafından kullanılıyor.';
            if (n === 'NotAllowedError' || n === 'SecurityError') return dev + ' izni reddedildi.';
            if (n === 'NotFoundError' || n === 'DevicesNotFoundError') return dev + ' bulunamadı.';
            if (n === 'OverconstrainedError' || n === 'ConstraintNotSatisfiedError') return dev + ' seçilen ayarı desteklemiyor.';
            return dev + ' açılamadı.';
        }
        // Tek bir track al ('audio' | 'video'); 'none' ise null; hata olursa null + uyarı (graceful degrade).
        async function acquireTrack(kind) {
            var off = (kind === 'audio') ? (prefs.micId === 'none') : (prefs.camId === 'none');
            if (off) return null;
            var constraints = (kind === 'audio') ? { audio: audioConstraint() } : { video: videoConstraint() };
            try {
                var s = await navigator.mediaDevices.getUserMedia(constraints);
                return (kind === 'audio') ? s.getAudioTracks()[0] : s.getVideoTracks()[0];
            } catch (e) {
                toast(mediaErr(kind, e), 'error');
                return null;
            }
        }

        function applyOutput() {
            var muted = (prefs.speakerId === 'none');
            try { ui.remote.muted = muted; ui.remote.volume = muted ? 0 : prefs.volume; } catch (e) {}
            if (!muted && canSetSink && prefs.speakerId) ui.remote.setSinkId(prefs.speakerId).catch(function () {});
        }
        function setVolume(v) {
            prefs.volume = Math.min(1, Math.max(0, v)); savePrefs();
            applyOutput();
        }

        // Yerel önizleme + sender track yönetimi
        function setLocalTrack(kindStr, track) {
            if (!localStream) localStream = new MediaStream();
            localStream.getTracks().filter(function (t) { return t.kind === kindStr; }).forEach(function (t) { t.stop(); localStream.removeTrack(t); });
            if (track) localStream.addTrack(track);
            ui.local.srcObject = localStream;
            if (kindStr === 'video') ui.local.hidden = !track;
        }
        function syncDeviceButtons() {
            var aOn = !!(audioSender && audioSender.track && audioSender.track.enabled);
            var vOn = !!(videoSender && videoSender.track && videoSender.track.enabled);
            ui.mute.classList.toggle('off', !aOn);
            ui.mmute.classList.toggle('off', !aOn);
            ui.cam.classList.toggle('off', !vOn);
        }
        // Kamera durumunu eşe bildir (eşte avatar/video gösterimi için — replaceTrack(null) mute olayını güvenilir tetiklemediğinden).
        function isVideoOn() { return !!(videoSender && videoSender.track && videoSender.track.enabled); }
        function sendVideoState() {
            if (callId) conn.invoke('NotifyMediaState', callId, isVideoOn()).catch(function () {});
        }
        function applyBitrate() {
            if (!videoSender || !videoPolicy || !videoPolicy.enabled || !(videoPolicy.maxBitrateKbps > 0)) return;
            try {
                var p = videoSender.getParameters();
                if (!p.encodings || !p.encodings.length) p.encodings = [{}];
                p.encodings[0].maxBitrate = videoPolicy.maxBitrateKbps * 1000;
                videoSender.setParameters(p);
            } catch (e) {}
        }

        // Sender'ları kur (caller addTransceiver yapar; callee SRD sonrası getTransceivers'tan bulur).
        function findSender(kind) {
            if (!pc) return null;
            var t = pc.getTransceivers().find(function (tr) {
                var rk = tr.receiver && tr.receiver.track && tr.receiver.track.kind;
                var sk = tr.sender && tr.sender.track && tr.sender.track.kind;
                return rk === kind || sk === kind;
            });
            return t ? t.sender : null;
        }
        async function attachLocalTracks(wantVideo) {
            var aTrack = await acquireTrack('audio');
            var vTrack = wantVideo ? await acquireTrack('video') : null;
            if (audioSender) { try { await audioSender.replaceTrack(aTrack); } catch (e) {} }
            if (videoSender) { try { await videoSender.replaceTrack(vTrack); } catch (e) {} }
            // yerel akış (önizleme + cleanup)
            localStream = new MediaStream();
            if (aTrack) localStream.addTrack(aTrack);
            if (vTrack) localStream.addTrack(vTrack);
            ui.local.srcObject = localStream;
            ui.local.hidden = !vTrack;
            applyBitrate();
            syncDeviceButtons();
        }
        // Canlı cihaz değişimi (çağrı sırasında, renegotiation YOK).
        async function applyDevice(kind, id) {
            if (kind === 'spk') { prefs.speakerId = id; savePrefs(); applyOutput(); return; }
            if (kind === 'mic') prefs.micId = id; else prefs.camId = id;
            savePrefs();
            var sender = (kind === 'mic') ? audioSender : videoSender;
            var kindStr = (kind === 'mic') ? 'audio' : 'video';
            if (!pc || !sender) { return; } // çağrı yoksa pref bir sonraki aramada uygulanır
            if (id === 'none') {
                try { await sender.replaceTrack(null); } catch (e) {}
                setLocalTrack(kindStr, null);
            } else {
                var track = await acquireTrack(kindStr);
                if (!track) return; // hata zaten toast'landı
                try { await sender.replaceTrack(track); } catch (e) {}
                setLocalTrack(kindStr, track);
                if (kind === 'cam') applyBitrate();
            }
            syncDeviceButtons();
            if (kind === 'cam') sendVideoState();
        }

        async function listDevices() {
            var devices = [];
            try { devices = await navigator.mediaDevices.enumerateDevices(); } catch (e) { return { mics: [], cams: [], speakers: [] }; }
            return {
                mics: devices.filter(function (d) { return d.kind === 'audioinput'; }),
                cams: devices.filter(function (d) { return d.kind === 'videoinput'; }),
                speakers: devices.filter(function (d) { return d.kind === 'audiooutput'; })
            };
        }

        var devModal = null;
        var deviceCache = null;     // {mics,cams,speakers} — oturum boyunca bellekte (sayfa yenilenince/çıkışta sıfırlanır)
        var scanAttempted = false;  // ilk tarama denendi mi? (izin reddedilse bile bir daha otomatik sorma)

        // Cihazları tara: etiketler için bir kez izin + enumerateDevices. force=false ve önbellek varsa onu döndürür.
        async function scanDevices(force) {
            if (deviceCache && !force) return deviceCache;
            // Etiketleri açmak için bir kez izin (aktif çağrıda zaten izinli → atla). Kamera etiketleri için video da iste.
            if (!localStream) {
                try { var s = await navigator.mediaDevices.getUserMedia({ audio: true, video: true }); s.getTracks().forEach(function (t) { t.stop(); }); }
                catch (e) { try { var s2 = await navigator.mediaDevices.getUserMedia({ audio: true }); s2.getTracks().forEach(function (t) { t.stop(); }); } catch (e2) {} }
            }
            scanAttempted = true;
            deviceCache = await listDevices();
            return deviceCache;
        }

        function buildDevModal() {
            var m = document.createElement('div');
            m.className = 'device-modal';
            m.hidden = true;
            m.innerHTML =
                '<div class="dev-card">' +
                  '<div class="dev-head"><span>Ayarlar</span><button type="button" class="dev-close" aria-label="Kapat">✕</button></div>' +
                  '<div class="dev-row"><label>Ses seviyesi</label><input type="range" class="dev-vol" min="0" max="100" step="1"></div>' +
                  '<div class="dev-row"><label>Mikrofon</label><select class="dev-mic"></select></div>' +
                  '<div class="dev-row dev-row--cam"><label>Kamera</label><select class="dev-cam"></select></div>' +
                  '<div class="dev-row dev-row--spk"><label>Hoparlör</label><select class="dev-spk"></select></div>' +
                  '<div class="dev-row"><button type="button" class="dev-rescan btn-outline">🔄 Cihazlarımı Algıla</button></div>' +
                  '<div class="dev-row dev-row--legal">' +
                    '<label>Yasal</label>' +
                    '<div class="dev-legal-actions">' +
                      '<button type="button" class="dev-legal btn-outline" data-doc="agreement">Üyelik Sözleşmesi</button>' +
                      '<button type="button" class="dev-legal btn-outline" data-doc="privacy">Gizlilik Politikası</button>' +
                    '</div>' +
                  '</div>' +
                '</div>';
            document.body.appendChild(m);
            m.addEventListener('click', function (e) { if (e.target === m) m.hidden = true; });
            m.querySelector('.dev-close').addEventListener('click', function () { m.hidden = true; });
            var vol = m.querySelector('.dev-vol');
            vol.addEventListener('input', function () { setVolume(parseInt(vol.value, 10) / 100); });
            m.querySelector('.dev-mic').addEventListener('change', function (e) { applyDevice('mic', e.target.value); });
            m.querySelector('.dev-cam').addEventListener('change', function (e) { applyDevice('cam', e.target.value); });
            m.querySelector('.dev-spk').addEventListener('change', function (e) { applyDevice('spk', e.target.value); });
            var rescan = m.querySelector('.dev-rescan');
            rescan.addEventListener('click', async function () {
                rescan.disabled = true; var old = rescan.textContent; rescan.textContent = 'Algılanıyor…';
                refillDevModal(await scanDevices(true));   // kullanıcı isteğiyle yeniden tara
                rescan.disabled = false; rescan.textContent = old;
            });
            m.querySelectorAll('.dev-legal').forEach(function (b) {
                b.addEventListener('click', function () { openLegal(b.getAttribute('data-doc')); });
            });
            if (!canSetSink) m.querySelector('.dev-row--spk').style.display = 'none';
            return m;
        }

        // ── Yasal belgeler: ayarların üstünde ikinci katman ─────────────────
        // Belge kendi sayfasından çekilir (tek kaynak: /Home/Agreement, /Home/Privacy);
        // metin burada kopyalanmaz, sayfa güncellenince modal da güncellenir.
        // Kapatınca ayarlar modalı geri açılır — kullanıcı başladığı yere döner.
        var legalModal = null;
        var legalCache = {};
        var LEGAL_DOCS = {
            agreement: { url: '/Home/Agreement', title: 'Üyelik Sözleşmesi' },
            privacy:   { url: '/Home/Privacy',   title: 'Gizlilik Politikası' }
        };

        function closeLegal() {
            if (!legalModal) return;
            legalModal.hidden = true;
            if (devModal) devModal.hidden = false;   // ilk modalı geri getir
        }

        function buildLegalModal() {
            var m = document.createElement('div');
            m.className = 'device-modal legal-modal';
            m.hidden = true;
            m.innerHTML =
                '<div class="dev-card legal-modal-card" role="dialog" aria-modal="true">' +
                  '<div class="dev-head"><span class="legal-modal-title"></span>' +
                  '<button type="button" class="dev-close" aria-label="Kapat">✕</button></div>' +
                  '<div class="legal-modal-body" tabindex="0"></div>' +
                '</div>';
            document.body.appendChild(m);
            m.addEventListener('click', function (e) { if (e.target === m) closeLegal(); });
            m.querySelector('.dev-close').addEventListener('click', closeLegal);
            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && legalModal && !legalModal.hidden) closeLegal();
            });
            return m;
        }

        async function openLegal(doc) {
            var meta = LEGAL_DOCS[doc];
            if (!meta) return;
            if (!legalModal) legalModal = buildLegalModal();

            legalModal.querySelector('.legal-modal-title').textContent = meta.title;
            var body = legalModal.querySelector('.legal-modal-body');
            if (devModal) devModal.hidden = true;
            legalModal.hidden = false;

            if (legalCache[doc]) { body.innerHTML = legalCache[doc]; body.scrollTop = 0; return; }

            body.textContent = 'Yükleniyor…';
            try {
                var res = await fetch(meta.url, { credentials: 'same-origin' });
                if (!res.ok) throw new Error('HTTP ' + res.status);
                var parsed = new DOMParser().parseFromString(await res.text(), 'text/html');
                var card = parsed.querySelector('.legal-card');
                if (!card) throw new Error('legal-card yok');
                // Sayfadaki h1 modal başlığında zaten duruyor; gövdede tekrar etmesin.
                var h1 = card.querySelector('h1'); if (h1) h1.remove();
                // Aynı köken olsa da gövdeye script/stil tasimayalim.
                card.querySelectorAll('script,style,link').forEach(function (n) { n.remove(); });
                legalCache[doc] = card.innerHTML;
                body.innerHTML = legalCache[doc];
                body.scrollTop = 0;
            } catch (e) {
                body.innerHTML = 'Belge yüklenemedi. <a href="' + meta.url +
                                 '" target="_blank" rel="noopener">Yeni sekmede aç</a>';
            }
        }
        function fillSelect(sel, items, selectedId, fallback) {
            sel.innerHTML = '';
            var none = document.createElement('option');
            none.value = 'none'; none.textContent = 'Yok (kapalı)';
            if (selectedId === 'none') none.selected = true;
            sel.appendChild(none);
            items.forEach(function (dev, i) {
                var o = document.createElement('option');
                o.value = dev.deviceId;
                o.textContent = dev.label || (fallback + ' ' + (i + 1));
                if (dev.deviceId === selectedId) o.selected = true;
                sel.appendChild(o);
            });
        }
        function refillDevModal(d) {
            d = d || { mics: [], cams: [], speakers: [] };
            fillSelect(devModal.querySelector('.dev-mic'), d.mics, prefs.micId, 'Mikrofon');
            fillSelect(devModal.querySelector('.dev-cam'), d.cams, prefs.camId, 'Kamera');
            if (canSetSink) fillSelect(devModal.querySelector('.dev-spk'), d.speakers, prefs.speakerId, 'Hoparlör');
            devModal.querySelector('.dev-vol').value = Math.round(prefs.volume * 100);
        }
        async function openDeviceSettings() {
            if (!devModal) devModal = buildDevModal();
            // Önbellek varsa yeniden TARAMA YOK; yoksa yalnızca ilk kez tara (her ayarlar açılışında tekrar taranmaz).
            var d = deviceCache || (!scanAttempted ? await scanDevices(false) : { mics: [], cams: [], speakers: [] });
            refillDevModal(d);
            devModal.hidden = false;
        }
        if (navigator.mediaDevices) {
            navigator.mediaDevices.addEventListener('devicechange', async function () {
                deviceCache = null; // donanım takıldı/çıkarıldı → önbelleği geçersiz kıl
                if (devModal && !devModal.hidden) refillDevModal(await scanDevices(true));
            });
        }

        async function getCallConfig() {
            var r = await B.api('/api/v1/call/config');
            if (!(r && r.success && r.data && Array.isArray(r.data.iceServers) && r.data.iceServers.length)) return null;
            videoPolicy = r.data.videoPolicy || null;
            relayOnly = (r.data.relayOnly !== false); // varsayılan güvenli taraf: relay
            // Port 53'ü bazı tarayıcılar engeller; relay'de STUN kullanılmaz, yine de filtrele. (:5349 gibi portları koru)
            var ice = r.data.iceServers.map(function (s) {
                var urls = (s.urls || []).filter(function (u) { return !/:53(\D|$)/.test(u); });
                return { urls: urls, username: s.username, credential: s.credential };
            }).filter(function (s) { return s.urls.length; });
            return ice.length ? ice : null;
        }

        function createPeer(iceServers) {
            // relayOnly: 'relay' → yalnızca TURN relay adayları (IP gizli); 'all' → P2P'ye de izin (yerel/STUN).
            pc = new RTCPeerConnection({ iceServers: iceServers, iceTransportPolicy: relayOnly ? 'relay' : 'all' });
            remoteStream = new MediaStream();
            ui.remote.srcObject = remoteStream;
            ui.overlay.dataset.remoteVideo = 'off'; // uzak kamera durumu (avatar/video seçimi)
            pc.onicecandidate = function (e) {
                if (e.candidate && callId) conn.invoke('SendIceCandidate', callId, JSON.stringify(e.candidate)).catch(function () {});
            };
            // transceiver+replaceTrack'te e.streams boş olabilir → track'leri kendi akışımıza biriktir.
            pc.ontrack = function (e) {
                try { if (remoteStream && !remoteStream.getTracks().includes(e.track)) remoteStream.addTrack(e.track); } catch (err) {}
                if (e.track.kind === 'video') {
                    e.track.onmute = refreshRemoteVideo;
                    e.track.onunmute = refreshRemoteVideo;
                    e.track.onended = refreshRemoteVideo;
                    refreshRemoteVideo();
                } else if (e.track.kind === 'audio') {
                    startVad();
                }
                applyOutput();
            };
            pc.oniceconnectionstatechange = function () { if (pc) console.log('[call] ICE:', pc.iceConnectionState); };
            pc.onconnectionstatechange = function () {
                if (!pc) return;
                console.log('[call] conn:', pc.connectionState);
                if (pc.connectionState === 'connected') refreshRemoteVideo();
                // Bağlantı kurulamadı/koptu: yalnız yerelde kapatma; HangUp ile eşe de CallEnded gönder
                // (aksi halde karşı taraf aramada asılı kalır).
                if (pc.connectionState === 'failed') hangup('Bağlantı kurulamadı.');
            };
        }

        // Uzak kamera açık mı? (avatar vs video gösterimi)
        function refreshRemoteVideo() {
            var live = false;
            if (pc) {
                pc.getReceivers().forEach(function (r) {
                    if (r.track && r.track.kind === 'video' && r.track.readyState === 'live' && !r.track.muted) live = true;
                });
            }
            ui.overlay.dataset.remoteVideo = live ? 'on' : 'off';
        }

        // Konuşma göstergesi: uzak sesi analiz et, eşik üstündeyse avatar border'ını yak.
        function startVad() {
            stopVad();
            if (!remoteStream) return;
            var aTracks = remoteStream.getAudioTracks();
            if (!aTracks.length) return;
            var ctx = ensureAudio();
            if (!ctx) return;
            try {
                vadSource = ctx.createMediaStreamSource(new MediaStream([aTracks[0]]));
                vadAnalyser = ctx.createAnalyser();
                vadAnalyser.fftSize = 512;
                vadSource.connect(vadAnalyser); // destination'a bağlanmaz (yalnız analiz)
                var data = new Uint8Array(vadAnalyser.fftSize);
                var smooth = 0;
                var loop = function () {
                    if (!vadAnalyser) return;
                    vadAnalyser.getByteTimeDomainData(data);
                    var sum = 0;
                    for (var i = 0; i < data.length; i++) { var v = (data[i] - 128) / 128; sum += v * v; }
                    smooth = smooth * 0.8 + Math.sqrt(sum / data.length) * 0.2;
                    ui.avatar.classList.toggle('speaking', smooth > 0.04);
                    vadRAF = requestAnimationFrame(loop);
                };
                loop();
            } catch (e) {}
        }
        function stopVad() {
            if (vadRAF) { cancelAnimationFrame(vadRAF); vadRAF = null; }
            if (vadSource) { try { vadSource.disconnect(); } catch (e) {} vadSource = null; }
            vadAnalyser = null;
            ui.avatar.classList.remove('speaking');
        }

        async function drainCandidates() {
            for (var i = 0; i < pendingCandidates.length; i++) {
                try { await pc.addIceCandidate(pendingCandidates[i]); } catch (e) {}
            }
            pendingCandidates = [];
        }

        async function startCall(targetId, type) {
            if (callId || incoming) { toast('Zaten bir aramadasınız.', 'error'); return; }
            isCaller = true; callType = type; peerId = targetId;
            try {
                var ice = await getCallConfig();
                if (!ice) { toast('Arama altyapısı şu an kullanılamıyor.', 'error'); resetState(); return; }
                createPeer(ice);
                audioSender = pc.addTransceiver('audio', { direction: 'sendrecv' }).sender;
                videoSender = (type === 'video') ? pc.addTransceiver('video', { direction: 'sendrecv' }).sender : null;
                await attachLocalTracks(type === 'video');
                var offer = await pc.createOffer();
                await pc.setLocalDescription(offer);
                showOverlay('calling', targetId, type);
                startRinging('ringback');
                callId = await conn.invoke('CallUser', targetId, type === 'video' ? 'Video' : 'Audio', JSON.stringify(offer));
                if (!callId) { endCall(); return; }     // CallError ayrı olarak toast'lar
                ringTimeout = setTimeout(function () {
                    if (callId && isCaller) { conn.invoke('CancelCall', callId).catch(function () {}); endCall('Cevap yok.'); }
                }, 45000);
            } catch (e) {
                toast('Arama başlatılamadı.', 'error'); endCall();
            }
        }

        conn.on('IncomingCall', function (data) {
            if (callId || incoming) { conn.invoke('RejectCall', data.callId).catch(function () {}); return; } // meşgul
            incoming = data; // { callId, callerId, callType, offer }
            showOverlay('incoming', data.callerId, (data.callType || '').toLowerCase() === 'video' ? 'video' : 'audio');
            startRinging('incoming');
        });

        async function acceptIncoming() {
            var data = incoming;
            if (!data) return;
            isCaller = false;
            callType = (data.callType || '').toLowerCase() === 'video' ? 'video' : 'audio';
            callId = data.callId; peerId = data.callerId; incoming = null;
            stopRinging();
            try {
                var ice = await getCallConfig();
                if (!ice) { conn.invoke('RejectCall', data.callId).catch(function () {}); endCall('Arama altyapısı kullanılamıyor.'); return; }
                createPeer(ice);
                // Önce remote description → transceiver'lar offer'dan oluşur (m-line hizası garanti).
                await pc.setRemoteDescription(JSON.parse(data.offer));
                audioSender = findSender('audio');
                videoSender = findSender('video');
                await attachLocalTracks(callType === 'video');
                await drainCandidates();
                var answer = await pc.createAnswer();
                await pc.setLocalDescription(answer);
                await conn.invoke('AnswerCall', data.callId, JSON.stringify(answer));
                showOverlay('in-call', peerId, callType);
                sendVideoState();
            } catch (e) {
                toast('Arama kabul edilemedi.', 'error');
                conn.invoke('RejectCall', data.callId).catch(function () {});
                endCall();
            }
        }

        function rejectIncoming() {
            if (!incoming) return;
            conn.invoke('RejectCall', incoming.callId).catch(function () {});
            incoming = null; stopRinging(); hideOverlay(); resetState();
        }

        conn.on('CallAnswered', async function (data) {
            if (data.callId !== callId || !pc) return;
            clearTimeout(ringTimeout);
            stopRinging();
            try {
                await pc.setRemoteDescription(JSON.parse(data.answer));
                await drainCandidates();
                showOverlay('in-call', peerId, callType);
                sendVideoState();
            } catch (e) { hangup('Bağlantı hatası.'); } // eşe de bildir, asılı kalmasın
        });

        conn.on('ReceiveIceCandidate', async function (data) {
            if (data.callId !== callId || !pc) return;
            var cand;
            try { cand = JSON.parse(data.candidate); } catch (e) { return; }
            if (pc.remoteDescription && pc.remoteDescription.type) {
                try { await pc.addIceCandidate(cand); } catch (e) {}
            } else {
                pendingCandidates.push(cand);
            }
        });

        conn.on('CallRejected', function (data) { if (data.callId === callId) endCall('Arama reddedildi.'); });
        conn.on('CallCanceled', function (data) {
            if (incoming && data.callId === incoming.callId) { incoming = null; stopRinging(); hideOverlay(); resetState(); toast('Cevapsız arama.'); }
            else if (data.callId === callId) endCall('Arama iptal edildi.');
        });
        conn.on('CallEnded', function (data) {
            // Henüz kabul edilmemiş gelen arama da kapansın (arayan HangUp ile bıraksa bile).
            if (incoming && data.callId === incoming.callId) { incoming = null; stopRinging(); hideOverlay(); resetState(); return; }
            if (data.callId === callId) endCall('Görüşme sonlandı.');
        });
        conn.on('CallError', function (msg) { toast(msg, 'error'); endCall(); });
        // Eşin kamera durumu (açık sinyal — mute heuristiğinden güvenilir): avatar/video gösterimini sürer.
        conn.on('CallMediaState', function (data) {
            if (data && data.callId === callId) ui.overlay.dataset.remoteVideo = data.videoOn ? 'on' : 'off';
        });

        function hangup(msg) {
            if (callId) conn.invoke('HangUp', callId).catch(function () {});
            endCall(msg);
        }
        // Kapat butonu mantığı: gelen aramayı reddet / çalan giden aramayı iptal et / görüşmeyi sonlandır.
        function terminate() {
            if (incoming) { rejectIncoming(); return; }                              // aranan: gelen aramayı reddet
            if (isCaller && callId && ui.overlay.dataset.mode === 'calling') {
                conn.invoke('CancelCall', callId).catch(function () {});             // arayan: cevaplanmadan iptal → eşe CallCanceled
                endCall();
            } else {
                hangup();                                                            // görüşme sürüyor → HangUp
            }
        }
        function endCall(msg) {
            clearTimeout(ringTimeout);
            stopRinging();
            cleanup();
            hideOverlay();
            if (msg) toast(msg);
        }
        function cleanup() {
            stopVad();
            if (localStream) { localStream.getTracks().forEach(function (t) { t.stop(); }); localStream = null; }
            remoteStream = null;
            if (pc) { try { pc.close(); } catch (e) {} pc = null; }
            audioSender = null; videoSender = null;
            resetState();
        }
        function resetState() {
            callId = null; peerId = null; isCaller = false; pendingCandidates = []; incoming = null;
        }

        function toggleMute() {
            if (!audioSender || !audioSender.track) { toast('Mikrofon kapalı (Ayarlar\'dan açabilirsiniz).'); return; }
            audioSender.track.enabled = !audioSender.track.enabled;
            syncDeviceButtons();
        }
        ui.mute.addEventListener('click', toggleMute);
        ui.mmute.addEventListener('click', toggleMute);
        ui.cam.addEventListener('click', function () {
            if (!videoSender || !videoSender.track) { toast('Kamera kapalı (Ayarlar\'dan açabilirsiniz).'); return; }
            videoSender.track.enabled = !videoSender.track.enabled;
            syncDeviceButtons();
            sendVideoState();
        });
        ui.accept.addEventListener('click', acceptIncoming);
        ui.cog.addEventListener('click', function () { openDeviceSettings(); });
        ui.hang.addEventListener('click', terminate);
        ui.mhang.addEventListener('click', terminate);

        // PiP küçült/büyüt + sürükle
        ui.min.addEventListener('click', function () { setMinimized(true); });
        ui.expand.addEventListener('click', function () { setMinimized(false); });
        (function () {
            var dragging = false, moved = false, sx = 0, sy = 0, offX = 0, offY = 0;
            ui.box.addEventListener('pointerdown', function (e) {
                if (!minimized) return;
                if (e.target.closest('.call-mini')) return; // mini butonlar kendi işini yapsın
                dragging = true; moved = false; sx = e.clientX; sy = e.clientY;
                var r = ui.box.getBoundingClientRect();
                offX = e.clientX - r.left; offY = e.clientY - r.top;
                try { ui.box.setPointerCapture(e.pointerId); } catch (er) {}
                e.preventDefault();
            });
            ui.box.addEventListener('pointermove', function (e) {
                if (!dragging) return;
                if (Math.abs(e.clientX - sx) > 4 || Math.abs(e.clientY - sy) > 4) moved = true;
                placeWidget(e.clientX - offX, e.clientY - offY);
            });
            ui.box.addEventListener('pointerup', function (e) {
                if (!dragging) return;
                dragging = false;
                try { ui.box.releasePointerCapture(e.pointerId); } catch (er) {}
                // Sürüklenmediyse (temiz tık) ve uzak video/avatara basıldıysa → büyüt
                if (!moved && (e.target === ui.remote || ui.poster.contains(e.target))) setMinimized(false);
            });
        })();

        // Sidebar'daki çağrı-öncesi ses/cihaz ayarları butonu.
        var btnAudioSettings = document.getElementById('audioSettingsBtn');
        if (btnAudioSettings) btnAudioSettings.addEventListener('click', function () { openDeviceSettings(); });

        // İlk girişte cihazları yalnızca BİR kez tara; sonraki ayarlar açılışları önbellekten gelir.
        // Reddedilse bile tekrar otomatik sorulmaz (manuel "Cihazlarımı Algıla" ile yenilenebilir).
        setTimeout(function () { if (!scanAttempted) scanDevices(false); }, 1500);

        var btnAudio = document.getElementById('callAudioBtn');
        var btnVideo = document.getElementById('callVideoBtn');
        if (btnAudio) btnAudio.addEventListener('click', function () { var id = B.currentFriendId(); if (id) startCall(id, 'audio'); });
        if (btnVideo) btnVideo.addEventListener('click', function () { var id = B.currentFriendId(); if (id) startCall(id, 'video'); });

        // Bir sohbet açıldığında arama butonlarını göster.
        document.addEventListener('chat:conversationchanged', function () {
            if (btnAudio) btnAudio.hidden = false;
            if (btnVideo) btnVideo.hidden = false;
        });

        // Autoplay kısıtı: ilk kullanıcı etkileşiminde ses bağlamını aç (gelen aramada zil çalabilsin).
        document.addEventListener('pointerdown', function unlock() {
            ensureAudio();
            document.removeEventListener('pointerdown', unlock);
        });

        // Sayfa kapanırken aktif aramayı düşür.
        window.addEventListener('beforeunload', function () {
            if (callId) { try { conn.invoke('HangUp', callId); } catch (e) {} }
        });
    }

    boot();
})();
