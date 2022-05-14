importScripts("https://www.gstatic.com/firebasejs/9.7.0/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/9.7.0/firebase-messaging-compat.js");
importScripts("/js/FirebaseConfig.js");

const app = firebase.initializeApp(firebaseConfig);

const messaging = firebase.messaging(app);

messaging.onBackgroundMessage(async (payload) => {
    console.info('Received firebase background message ', payload);

    //Shouldn't need to do anything here, the FCM message payload already has a Notification setup
});

console.log("Service worker initialized");

this.addEventListener('message', async message => {
    console.info("Service worker receiving message.");

    if (message.data === "GetToken") {
        console.info("Service worker: GetToken message");

        const token = await messaging.getToken({
            serviceWorkerRegistration: registration
        });

        console.info("Service worker token: " + token);

        message.source.postMessage(token);
    }
});

this.addEventListener('install', function (event) {
    event.waitUntil(skipWaiting());
});

this.addEventListener('activate', function (event) {
    event.waitUntil(clients.claim());
});