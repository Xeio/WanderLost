package com.xeio.lostmerchants

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.activity.ComponentActivity

class OpenBrowser : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val openBrowserIntent = Intent(Intent.ACTION_VIEW)
        openBrowserIntent.data = Uri.parse("https://lostmerchants.com")
        startActivity(openBrowserIntent)

        finish()
    }
}