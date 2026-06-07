/* Tural Chat — Service Worker (PWA shell + offline) */
const CACHE = 'tural-chat-v7';

// App shell — sade path'ler (sürümsüz). Runtime cache ?v'li istekleri ayrıca yakalar.
const SHELL = [
    '/offline.html',
    '/css/theme.css',
    '/css/chat.css',
    '/js/ft-time.js',
    '/js/theme.js',
    '/js/toast.js',
    '/js/auth.js',
    '/js/chat.js',
    '/js/call.js',
    '/js/profile.js',
    '/js/consent.js',
    '/js/pwa.js',
    '/site.webmanifest',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/favicon.ico'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE)
            // Tek tek ekle; biri 404 olsa bile install patlamasın.
            .then((cache) => Promise.allSettled(SHELL.map((url) => cache.add(url))))
            .then(() => self.skipWaiting())
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
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/hubs/')) return;

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
