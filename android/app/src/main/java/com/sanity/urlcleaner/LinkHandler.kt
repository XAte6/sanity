package com.sanity.urlcleaner

import android.content.Context

data class LinkHandleResult(
    val finalUrl: String,
    val cleaned: Boolean
)

object LinkHandler {
    fun handle(context: Context, url: String): LinkHandleResult {
        val config = AppConfigStore.load(context)
        var finalUrl = url
        var cleaned = false

        if (config.isLinkProxyActive) {
            UrlCleaner.tryClean(finalUrl, config.rules)?.let {
                finalUrl = it
                cleaned = true
            }
        }

        BrowserForwarder.open(context, finalUrl, config.targetBrowser)
        return LinkHandleResult(finalUrl = finalUrl, cleaned = cleaned)
    }
}
