package com.sanity.urlcleaner

import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri

data class BrowserInfo(
    val label: String,
    val packageName: String
)

object BrowserForwarder {
    fun installedBrowsers(context: Context): List<BrowserInfo> {
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://example.com"))
        val selfPackage = context.packageName
        val seen = linkedSetOf<String>()
        val browsers = mutableListOf<BrowserInfo>()

        val activities = context.packageManager.queryIntentActivities(
            intent,
            PackageManager.MATCH_DEFAULT_ONLY
        )

        for (resolveInfo in activities) {
            val packageName = resolveInfo.activityInfo.packageName
            if (packageName == selfPackage || !seen.add(packageName)) continue
            val label = resolveInfo.loadLabel(context.packageManager).toString()
            browsers.add(BrowserInfo(label = label, packageName = packageName))
        }

        return browsers.sortedBy { it.label.lowercase() }
    }

    fun open(context: Context, url: String, targetPackage: String) {
        val uri = Uri.parse(url)
        val intent = Intent(Intent.ACTION_VIEW, uri).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            if (targetPackage.isNotBlank()) {
                setPackage(targetPackage)
            }
        }

        if (intent.resolveActivity(context.packageManager) != null) {
            context.startActivity(intent)
            return
        }

        val fallback = Intent(Intent.ACTION_VIEW, uri).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        context.startActivity(fallback)
    }
}
