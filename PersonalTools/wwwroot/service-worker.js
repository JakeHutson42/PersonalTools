'use strict';

const staticCacheName = 'personal-tools-static-v4';
const caseImageCacheName = 'personal-tools-case-images-v1';
const maximumCaseImages = 250;
let caseImageWritesSinceTrim = 0;
const offlineFallbackUrl = '/offline.html';
const precacheUrls = [
    offlineFallbackUrl,
    '/manifest.webmanifest',
    '/case-tycoon.webmanifest',
    '/favicon.svg',
    '/css/pwa.css',
    '/icons/apple-touch-icon.png',
    '/icons/pwa-icon-192.png',
    '/icons/pwa-icon-512.png',
    '/icons/pwa-maskable-192.png',
    '/icons/pwa-maskable-512.png',
    '/icons/case-tycoon-apple-touch-icon.png',
    '/icons/case-tycoon-favicon-64.png',
    '/icons/case-tycoon-icon-192.png',
    '/icons/case-tycoon-icon-512.png',
    '/icons/case-tycoon-maskable-192.png',
    '/icons/case-tycoon-maskable-512.png'
];
const staticPathPrefixes = ['/css/', '/js/', '/images/', '/lib/', '/icons/'];

function staticCacheKey(url) {
    return new Request(`${url.origin}${url.pathname}`, { method: 'GET' });
}

function isSafeStaticRequest(request, url) {
    return url.origin === self.location.origin
        && request.method === 'GET'
        && !request.headers.has('range')
        && staticPathPrefixes.some(prefix => url.pathname.startsWith(prefix));
}

function isCaseImageRequest(request, url) {
    return request.destination === 'image'
        && request.method === 'GET'
        && url.hostname === 'community.akamai.steamstatic.com'
        && url.pathname.startsWith('/economy/image/');
}

async function trimCache(cache, maximumEntries) {
    const keys = await cache.keys();
    const overflow = keys.length - maximumEntries;
    if (overflow > 0) await Promise.all(keys.slice(0, overflow).map(key => cache.delete(key)));
}

self.addEventListener('install', event => {
    event.waitUntil(caches.open(staticCacheName).then(cache => cache.addAll(precacheUrls)));
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys
                .filter(key => (key.startsWith('personal-tools-static-') && key !== staticCacheName)
                    || (key.startsWith('personal-tools-case-images-') && key !== caseImageCacheName))
                .map(key => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('message', event => {
    if (event.data?.type === 'SKIP_WAITING') self.skipWaiting();
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    if (request.mode === 'navigate') {
        // Authenticated HTML always comes from the network. Only the generic offline page is cached.
        event.respondWith(fetch(request).catch(() => caches.match(offlineFallbackUrl)));
        return;
    }

    if (isCaseImageRequest(request, url)) {
        event.respondWith((async () => {
            const cache = await caches.open(caseImageCacheName);
            const cached = await cache.match(request, { ignoreVary: true });
            if (cached) return cached;
            try {
                const response = await fetch(request);
                if (response.ok || response.type === 'opaque') {
                    event.waitUntil((async () => {
                        await cache.put(request, response.clone());
                        caseImageWritesSinceTrim += 1;
                        if (caseImageWritesSinceTrim >= 20) {
                            caseImageWritesSinceTrim = 0;
                            await trimCache(cache, maximumCaseImages);
                        }
                    })());
                }
                return response;
            } catch {
                return Response.error();
            }
        })());
        return;
    }

    if (!isSafeStaticRequest(request, url)) return;

    event.respondWith((async () => {
        const cache = await caches.open(staticCacheName);
        const cacheKey = staticCacheKey(url);
        try {
            // HTTP caching keeps this inexpensive online while still making a newly versioned
            // asset available immediately. The private application shell never enters this path.
            const response = await fetch(request);
            const contentType = response.headers.get('content-type') || '';
            if (response.ok && response.type === 'basic' && !response.redirected && !contentType.includes('text/html')) {
                await cache.put(cacheKey, response.clone());
            }
            return response;
        } catch {
            return (await cache.match(cacheKey)) || Response.error();
        }
    })());
});
