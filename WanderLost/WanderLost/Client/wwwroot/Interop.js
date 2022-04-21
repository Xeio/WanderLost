
PlayNotificationSound = function () {
    document.getElementById('notifsound').play();
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