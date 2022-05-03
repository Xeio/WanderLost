async function clearLocalstorageCacheAndReload() {
    try {
        //If the blazor UI has an error, just forcibly clear the local storage cache when reloading
        //Some users saw instances where clearing the cached DLLs helped, possibly rare CDN issue when purging caches for new update?
        //Ideally this should be rare enough not to care about the extra requests it causes
        await caches.delete((await caches.keys())[0]);
    }
    catch {
        //Don't really care about errors clearing cache
    }
    location.reload();
}