
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
    return new Notification(title, options);
};

Dismiss = function (notification) {
    notification.close();
};