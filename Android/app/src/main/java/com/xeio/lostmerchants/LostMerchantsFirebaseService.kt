package com.xeio.lostmerchants

import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.widget.Toast
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.xeio.lostmerchants.api.PushApi
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

class LostMerchantsFirebaseService : FirebaseMessagingService() {
    override fun onNewToken(newToken: String) {
        super.onNewToken(newToken)
        Log.d("LostMerchants", "Refreshed token: $newToken")

        CoroutineScope(Dispatchers.IO).launch {
            PushApi.loanSavedSubscription(applicationContext)?.let {
                val oldToken = it.token
                if (newToken != oldToken) {
                    Log.d("LostMerchants", "New token, updating existing subscription $oldToken")
                    //New token and we have an old subscription, add subscription with new token and remove the old one
                    it.token = newToken
                    PushApi.retrofitService.updatePushSubscription(it)
                    PushApi.retrofitService.removePushSubscription(oldToken)
                    PushApi.savePushSubscriptionSettings(applicationContext, it)
                }
            }
        }
    }

    override fun onCreate() {
        super.onCreate()

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val notificationManager = getSystemService(NOTIFICATION_SERVICE) as NotificationManager

            val weiChannel = NotificationChannel("wei", getString(R.string.wei_channel), NotificationManager.IMPORTANCE_HIGH)
            weiChannel.description = getString(R.string.wei_channel_desc)
            notificationManager.createNotificationChannel(weiChannel)

            val rapportChannel = NotificationChannel("rapport", getString(R.string.rapport_channel), NotificationManager.IMPORTANCE_DEFAULT)
            rapportChannel.description = getString(R.string.rapport_channel_desc)
            notificationManager.createNotificationChannel(rapportChannel)
        }
    }

    override fun onMessageReceived(message: RemoteMessage) {
        Log.d("LostMerchants", "Got message in foreground")

        val handler = Handler(Looper.getMainLooper())
        handler.post {
            val toast = Toast.makeText(applicationContext, "Got notification in foreground, test again with app minimized", Toast.LENGTH_LONG)
            toast.show()
        }
    }
}