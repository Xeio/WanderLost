const worker = await navigator.serviceWorker.register('ServiceWorker.js', {
    scope: '/',
    updateViaCache: 'all'
});

worker.update();