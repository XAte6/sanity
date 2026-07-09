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
        val url = UrlCleaner.extractHttpUrl(text)
            ?: return ShareHandleResult(finalUrl = "", cleaned = false, shared = false)

        val clean = LinkHandler.cleanIf(context, url) { it.isActive }
        val shareIntent = Intent(Intent.ACTION_SEND).apply {
            type = "text/plain"
            putExtra(Intent.EXTRA_TEXT, clean.finalUrl)
        }

        context.startActivity(
            Intent.createChooser(shareIntent, context.getString(R.string.share_chooser_title))
        )

        return ShareHandleResult(
            finalUrl = clean.finalUrl,
            cleaned = clean.cleaned,
            shared = true
        )
    }
}
