package com.sanity.urlcleaner

import android.content.Context

data class CleanResult(
    val finalUrl: String,
    val cleaned: Boolean
)

data class LinkHandleResult(
    val finalUrl: String,
    val cleaned: Boolean,
    val opened: Boolean
)

object LinkHandler {
    fun cleanIf(context: Context, url: String, shouldClean: (AppConfig) -> Boolean): CleanResult {
        val config = AppConfigStore.load(context)
        var finalUrl = url
        var cleaned = false

        if (shouldClean(config)) {
            UrlCleaner.tryClean(finalUrl, config.rules)?.let {
                finalUrl = it
                cleaned = true
            }
        }

        return CleanResult(finalUrl = finalUrl, cleaned = cleaned)
    }

    fun handle(context: Context, url: String): LinkHandleResult {
        val config = AppConfigStore.load(context)
        val clean = cleanIf(context, url) { it.isLinkProxyActive }
        val opened = BrowserForwarder.open(context, clean.finalUrl, config.targetBrowser)
        return LinkHandleResult(
            finalUrl = clean.finalUrl,
            cleaned = clean.cleaned,
            opened = opened
        )
    }
}
