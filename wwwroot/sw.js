/* TropiValle PWA — Service Worker */
const CACHE = 'tropivalle-v1';
const PRECACHE = [
  '/',
  '/manifest.webmanifest',
  '/css/site.css',
  '/js/site.js',
  '/js/pwa.js',
  '/js/speech.js',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/images/logo-tropivalle.png'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE).then((cache) => cache.addAll(PRECACHE).catch(() => undefined))
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)))
    )
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  const req = event.request;
  if (req.method !== 'GET') return;

  const url = new URL(req.url);
  // Solo mismo origen
  if (url.origin !== self.location.origin) return;

  // Navegación: network first, fallback cache / offline
  if (req.mode === 'navigate') {
    event.respondWith(
      fetch(req)
        .then((res) => {
          const copy = res.clone();
          caches.open(CACHE).then((c) => c.put(req, copy));
          return res;
        })
        .catch(() =>
          caches.match(req).then((r) => r || caches.match('/') || offlinePage())
        )
    );
    return;
  }

  // Estáticos: cache first
  if (/\.(css|js|png|jpg|jpeg|webp|svg|woff2?|webmanifest)$/i.test(url.pathname)) {
    event.respondWith(
      caches.match(req).then(
        (cached) =>
          cached ||
          fetch(req).then((res) => {
            const copy = res.clone();
            caches.open(CACHE).then((c) => c.put(req, copy));
            return res;
          })
      )
    );
  }
});

function offlinePage() {
  return new Response(
    '<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/><title>Sin conexión — TropiValle</title><style>body{font-family:system-ui;background:#0a120e;color:#fff;display:flex;min-height:100vh;align-items:center;justify-content:center;text-align:center;padding:2rem}a{color:#9af00a}</style></head><body><div><h1>Sin conexión</h1><p>No hay internet. Revisa tu red e intenta de nuevo.</p><p><a href="/">Volver al inicio</a></p></div></body></html>',
    { headers: { 'Content-Type': 'text/html; charset=utf-8' } }
  );
}
