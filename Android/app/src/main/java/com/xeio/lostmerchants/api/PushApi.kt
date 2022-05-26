package com.xeio.lostmerchants.api

import android.content.Context
import androidx.preference.PreferenceManager
import com.google.gson.Gson
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.moshi.MoshiConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

private const val DOMAIN = "https://test.lostmerchants.com/"

private val moshi = Moshi.Builder()
    .add(KotlinJsonAdapterFactory())
    .build()

private val retrofit = Retrofit.Builder()
    .addConverterFactory(MoshiConverterFactory.create(moshi))
    .baseUrl(DOMAIN)
    .build()

interface PushSubscriptionsService {
    @POST("api/PushNotifications/GetPushSubscription")
    suspend fun getPushSubscription(@Body token: String): Response<PushSubscription>

    @POST("api/PushNotifications/UpdatePushSubscription")
    suspend fun updatePushSubscription(@Body subscription: PushSubscription)

    @POST("api/PushNotifications/RemovePushSubscription")
    suspend fun removePushSubscription(@Body token: String)

    @GET("data/servers.json")
    suspend fun getServers(): Map<String, ServerRegion>
}

object PushApi {
    val retrofitService : PushSubscriptionsService by lazy {
        retrofit.create(PushSubscriptionsService::class.java)
    }

    private const val PUSH_SETTINGS_KEY = "PushNotifications"

    fun loanSavedSubscription(context: Context) : PushSubscription? {
        val preferences = PreferenceManager.getDefaultSharedPreferences(context)
        preferences.getString(PUSH_SETTINGS_KEY, null)?.let{
            return Gson().fromJson(it, PushSubscription::class.java)
        }
        return null
    }

    fun savePushSubscriptionSettings(context: Context, pushSubscription: PushSubscription?) {
        val preferences = PreferenceManager.getDefaultSharedPreferences(context)
        if(pushSubscription != null) {
            pushSubscription.sendTestNotification = false //Never save test setting as true
            val subscriptionJson = Gson().toJson(pushSubscription)
            preferences.edit().putString(PUSH_SETTINGS_KEY, subscriptionJson).apply()
        }else{
            preferences.edit().remove(PUSH_SETTINGS_KEY).apply()
        }
    }
}