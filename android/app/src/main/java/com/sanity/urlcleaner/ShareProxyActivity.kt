package com.sanity.urlcleaner

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity

class ShareProxyActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val text = extractSharedText(intent)
        if (text.isNullOrBlank()) {
            Toast.makeText(this, R.string.share_no_url, Toast.LENGTH_LONG).show()
            finish()
            return
        }

        val config = AppConfigStore.load(this)
        val result = ShareHandler.handle(this, text)

        when {
            !result.shared -> {
                Toast.makeText(this, R.string.share_no_url, Toast.LENGTH_LONG).show()
            }
            result.cleaned && config.notificationsEnabled -> {
                Toast.makeText(this, R.string.tracking_removed_share, Toast.LENGTH_SHORT).show()
            }
        }

        finish()
    }

    override fun onNewIntent(intent: Intent?) {
        super.onNewIntent(intent)
        setIntent(intent)
        recreate()
    }

    private fun extractSharedText(intent: Intent?): String? {
        if (intent?.action != Intent.ACTION_SEND) return null

        intent.getStringExtra(Intent.EXTRA_TEXT)
            ?.trim()
            ?.takeIf { it.isNotEmpty() }
            ?.let { return it }

        val clip = intent.clipData ?: return null
        if (clip.itemCount == 0) return null

        return clip.getItemAt(0).text?.toString()?.trim()?.takeIf { it.isNotEmpty() }
    }
}
