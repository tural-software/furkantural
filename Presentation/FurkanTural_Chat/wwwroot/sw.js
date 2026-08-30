/* PWA kabuğu, çevrimdışı yedek ve bildirimler. */
const CACHE = 'chatural-v12';

// App shell — sade path'ler (sürümsüz). Runtime cache ?v'li istekleri ayrıca yakalar.
const SHELL = [
    '/offline.html',
    '/css/theme.css',
    '/css/chat.css',
    '/js/ft-time.js',
    '/js/theme.js',
    '/js/offline.js',
    '/js/toast.js',
    '/js/auth.js',
    '/js/chat.js',
    '/js/call.js',
    '/js/profile.js',
    '/js/consent.js',
    '/js/pwa.js',
    '/js/notifications.js',
    '/site.webmanifest',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/favicon.ico'
];

self.addEventListener('install', (event) => {
    // Yeni sürüm kendiliğinden devralmaz, bekler. Devralsaydı kullanıcı sohbetin ortasındayken sayfa
    // yenilenirdi. Devralma yalnızca kullanıcı güncellemeyi onayladığında, pwa.js’in gönderdiği
    // mesajla olur. İlk kurulumda bekleyen bir sürüm olmadığı için doğrudan etkinleşir.
    event.waitUntil(
        caches.open(CACHE)
            // Tek tek ekle; biri 404 olsa bile install patlamasın.
            .then((cache) => Promise.allSettled(SHELL.map((url) => cache.add(url))))
    );
});

// Prompt'lu güncelleme: kullanıcı onaylayınca bekleyen SW'yi etkinleştir.
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

self.addEventListener('push', (event) => {
    let data = {};
    try { data = event.data ? event.data.json() : {}; } catch (e) { data = {}; }
    const title = data.title || 'Chatural';
    const options = {
        body: data.body || 'Yeni bir mesajın var',
        tag: data.tag || 'chat-message',     // aynı tag → bildirimler üst üste yığılmaz
        renotify: true,
        icon: '/icons/icon-192.png',
        badge: '/icons/icon-192.png',
        data: { url: data.url || '/Chat' }
    };
    event.waitUntil(self.registration.showNotification(title, options));
});

// Bildirime tıklayınca açık pencereyi öne getir; yoksa uygulamayı aç.
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const url = (event.notification.data && event.notification.data.url) || '/Chat';
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then((wins) => {
            for (const c of wins) {
                if ('focus' in c) { c.focus(); return; }
            }
            if (clients.openWindow) return clients.openWindow(url);
        })
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const req = event.request;
    const url = new URL(req.url);

    // Sadece aynı-origin GET. API/SignalR ve cross-origin'e karışma.
    if (req.method !== 'GET' || url.origin !== self.location.origin) return;
    // Kimlikli veri (mesajlar, ekler) hiçbir koşulda önbelleğe alınmaz. Alınsaydı çıkıştan veya
    // kullanıcı değişiminden sonra bir öncekinin verisi önbellekten dönebilirdi.
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/hubs/') || url.pathname.startsWith('/bff/')) return;

    // Gezinmeler: network-first → çevrimdışı yedeği offline.html
    if (req.mode === 'navigate') {
        event.respondWith(
            fetch(req).catch(() => caches.match('/offline.html'))
        );
        return;
    }

    // Statik varlıklar: stale-while-revalidate
    event.respondWith(
        caches.open(CACHE).then((cache) =>
            cache.match(req).then((cached) => {
                const network = fetch(req)
                    .then((res) => {
                        if (res && res.status === 200 && res.type === 'basic') {
                            cache.put(req, res.clone());
                        }
                        return res;
                    })
                    .catch(() => cached);
                return cached || network;
            })
        )
    );
});
