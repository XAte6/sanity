package com.sanity.urlcleaner

import android.content.Context
import android.content.Intent

data class ShareHandleResult(
    val finalUrl: String,
    val cleaned: Boolean,
    val shared: Boolean
)

object ShareHandler {
    fun handle(context: Context, text: String): ShareHandleResult {
        val config = AppConfigStore.load(context)
        val url = UrlCleaner.extractHttpUrl(text)
            ?: return ShareHandleResult(finalUrl = "", cleaned = false, shared = false)

        val cleanedUrl = if (config.isActive) {
            UrlCleaner.tryClean(url, config.rules) ?: url
        } else {
            url
        }
        val wasCleaned = cleanedUrl != url
        if (wasCleaned) {
            UsageMetricsStore.recordClean(context, cleanedUrl)
        }

        val shareIntent = Intent(Intent.ACTION_SEND).apply {
            type = "text/plain"
            putExtra(Intent.EXTRA_TEXT, cleanedUrl)
        }

        context.startActivity(
            Intent.createChooser(shareIntent, context.getString(R.string.share_chooser_title))
        )

        return ShareHandleResult(
            finalUrl = cleanedUrl,
            cleaned = wasCleaned,
            shared = true
        )
    }
}
