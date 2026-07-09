package com.sanity.urlcleaner

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity

class LinkProxyActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val url = intent?.dataString
        if (url.isNullOrBlank()) {
            finish()
            return
        }

        val config = AppConfigStore.load(this)
        val result = LinkHandler.handle(this, url)

        if (result.cleaned && config.notificationsEnabled) {
            Toast.makeText(this, "Tracking removed from clicked URL.", Toast.LENGTH_SHORT).show()
        }

        finish()
    }

    override fun onNewIntent(intent: Intent?) {
        super.onNewIntent(intent)
        setIntent(intent)
        recreate()
    }
}
