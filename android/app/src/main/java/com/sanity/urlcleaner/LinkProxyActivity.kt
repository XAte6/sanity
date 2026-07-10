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

        when {
            !result.opened -> {
                Toast.makeText(this, R.string.browser_open_failed, Toast.LENGTH_LONG).show()
            }
            result.cleaned && config.notificationsEnabled -> {
                Toast.makeText(this, R.string.tracking_removed, Toast.LENGTH_SHORT).show()
            }
        }

        finish()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        recreate()
    }
}
