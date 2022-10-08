
PlayNotificationSound = function (volume) {
    let soundElement = document.getElementById('notifsound');
    soundElement.pause(); //No effect if already paused
    soundElement.currentTime = 0;
    soundElement.volume = volume;
    soundElement.play();
};

SupportsNotifications = function () {
    return Notification != undefined;
};

RequestPermission = async function () {
    return await Notification.requestPermission();
};

Create = function (title, options) {
    let notification = new Notification(title, options);
    notification.addEventListener('click', () => window.focus());
    return notification;
};

Dismiss = function (notification) {
    notification.close();
};

//Service worker messaging is push then response, so need to set up a promise to handle
let resolveTokenPromise;
GetServiceWorkerToken = async function () {
    try {
        //In case the browser fails to load the service worker in a timely manner, set a timeout
        //This mostly seems to happen in debug mode
        const timeout = new Promise((resolve, reject) => {
            setTimeout(() => { reject("timed out"); }, 5000);
        });

        await Promise.race([navigator.serviceWorker.ready, timeout]);

        let tokenPromise = new Promise((resolve, reject) => {
            resolveTokenPromise = resolve;
        });
        navigator.serviceWorker.controller.postMessage('GetToken');

        return await Promise.race([tokenPromise, timeout]);
    }
    catch (e) {
        return null;
    }
};

navigator.serviceWorker.addEventListener('message', message => {
    if (resolveTokenPromise) {
        resolveTokenPromise(message.data);
        resolveTokenPromise = undefined;
    }
});

ShowBodyScroll = function () {
    document.body.classList.remove('noscroll');
}

HideBodyScroll = function () {
    document.body.classList.add('noscroll');
}
