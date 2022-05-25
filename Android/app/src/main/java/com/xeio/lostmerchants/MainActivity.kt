package com.xeio.lostmerchants

import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.lifecycleScope
import com.google.firebase.messaging.FirebaseMessaging
import com.xeio.lostmerchants.api.PushApi
import com.xeio.lostmerchants.api.PushSubscription
import com.xeio.lostmerchants.api.ServerRegion
import com.xeio.lostmerchants.ui.theme.LostMerchantsTheme
import kotlinx.coroutines.launch
import kotlinx.coroutines.tasks.await

class MainActivity : ComponentActivity() {
    private var serverRegions: Map<String, ServerRegion> = emptyMap()
    private var pushSubscription: PushSubscription = PushSubscription()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        pushSubscription = PushApi.loanSavedSubscription(applicationContext) ?: PushSubscription()

        setContent {
            LostMerchantsTheme {
                // A surface container using the 'background' color from the theme
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colors.background
                ) {
                    MainLayout(serverRegions, pushSubscription)
                }
            }
        }

        lifecycleScope.launch {
            serverRegions = PushApi.retrofitService.getServers()

            val token = FirebaseMessaging.getInstance().token.await()

            Log.i("LostMerchants", "Token $token")

            val existingSubscription = PushApi.retrofitService.getPushSubscription(token).body()
            if (existingSubscription != null) {
                pushSubscription = existingSubscription
            } else {
                pushSubscription.token = token
            }

            setContent {
                LostMerchantsTheme {
                    // A surface container using the 'background' color from the theme
                    Surface(
                        modifier = Modifier.fillMaxSize()
                    ) {
                        MainLayout(serverRegions, pushSubscription)
                    }
                }
            }
        }
    }
}

@Composable
fun MainLayout(regions: Map<String, ServerRegion>, pushSubscription: PushSubscription) {
    var regionExpanded by remember { mutableStateOf(false) }
    var serverExpanded by remember { mutableStateOf(false) }
    var region by remember { mutableStateOf("") }

    var hasSubscription by remember { mutableStateOf(pushSubscription.server.isNotBlank()) }
    var modified by remember { mutableStateOf(false) }
    val context = LocalContext.current

    val (savedSubscription, setSub) = remember {
        mutableStateOf(
            pushSubscription,
            policy = neverEqualPolicy()
        )
    }

    val composableScope = rememberCoroutineScope()

    if (region.isEmpty()) {
        region = if (pushSubscription.server.isNotEmpty()) {
            regions.filter { it.value.Servers.contains(pushSubscription.server) }
                .firstNotNullOfOrNull { it.key } ?: ""
        } else {
            regions.firstNotNullOfOrNull { it.key } ?: ""
        }
    }

    Column(
        modifier = Modifier.padding(32.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(text = "Region")
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .border(0.5.dp, MaterialTheme.colors.onSurface.copy(alpha = 0.5f))
                .clickable(onClick = { regionExpanded = true })
        ) {
            Text(regions[region]?.Name ?: "")
            DropdownMenu(expanded = regionExpanded, onDismissRequest = { regionExpanded = false }) {
                regions.forEach {
                    DropdownMenuItem(onClick = {
                        region = it.key
                        regionExpanded = false
                    }) {
                        Text(it.value.Name)
                    }
                }
            }
        }
        Text(text = "Server")
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .border(0.5.dp, MaterialTheme.colors.onSurface.copy(alpha = 0.5f))
                .clickable(onClick = { serverExpanded = true })
        ) {
            Text(savedSubscription.server)
            DropdownMenu(
                expanded = serverExpanded,
                onDismissRequest = { serverExpanded = false },
            ) {
                regions[region]?.Servers?.forEach {
                    DropdownMenuItem(onClick = {
                        pushSubscription.server = it
                        modified = true
                        serverExpanded = false
                    }) {
                        Text(it)
                    }
                }
            }
        }
        Row {
            Text(text = "Wei Notify")
            Switch(
                checked = savedSubscription.weiNotify,
                onCheckedChange = {
                    pushSubscription.weiNotify = it
                    modified = true
                    setSub(pushSubscription)
                },
            )
        }
        if (savedSubscription.weiNotify) {
            TextField(
                value = savedSubscription.weiVoteThreshold.toString(),
                label = { Text("Wei Vote Threshold") },
                onValueChange = {
                    pushSubscription.weiVoteThreshold = it.toIntOrNull() ?: 0
                    modified = true
                    setSub(pushSubscription)
                },
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
            )
        }
        Row {
            Text(text = "Legendary Rapport Notify")
            Switch(
                checked = savedSubscription.legendaryRapportNotify,
                onCheckedChange = {
                    pushSubscription.legendaryRapportNotify = it
                    modified = true
                    setSub(pushSubscription)
                },
            )
        }
        if (savedSubscription.legendaryRapportNotify) {
            TextField(
                value = savedSubscription.rapportVoteThreshold.toString(),
                label = { Text("Legendary Rapport Vote Threshold") },
                onValueChange = {
                    pushSubscription.rapportVoteThreshold = it.toIntOrNull() ?: 0
                    modified = true
                    setSub(pushSubscription)
                },
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
            )
        }
        Button(onClick = {
            composableScope.launch {
                PushApi.retrofitService.updatePushSubscription(pushSubscription)
                PushApi.savePushSubscriptionSettings(context, pushSubscription)
                Toast.makeText(context, "Subscription updated", Toast.LENGTH_SHORT).show()
                modified = false
                hasSubscription = true
            }
        }, enabled = modified && pushSubscription.server.isNotBlank()) {
            Text("Update Subscription")
        }
        Button(onClick = {
            composableScope.launch {
                PushApi.retrofitService.removePushSubscription(pushSubscription.token)
                PushApi.savePushSubscriptionSettings(context, null)
                Toast.makeText(context, "Subscription deleted", Toast.LENGTH_SHORT).show()
                modified = true
                hasSubscription = false
            }
        }, enabled = hasSubscription) {
            Text("Delete Subscription")
        }
        Button(onClick = {
            composableScope.launch {
                pushSubscription.sendTestNotification = true
                PushApi.retrofitService.updatePushSubscription(pushSubscription)
                Toast.makeText(context, "Requesting test. Minimize app to get notification.", Toast.LENGTH_LONG).show()
                pushSubscription.sendTestNotification = false
                PushApi.savePushSubscriptionSettings(context, pushSubscription)
                modified = false
                hasSubscription = true
            }
        }) {
            Text("Send Test Notification")
        }
    }
}