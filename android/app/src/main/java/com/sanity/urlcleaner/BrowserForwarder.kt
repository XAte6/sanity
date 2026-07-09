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
        val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://example.com")).apply {
            addCategory(Intent.CATEGORY_BROWSABLE)
        }
        val selfPackage = context.packageName
        val seen = linkedSetOf<String>()
        val browsers = mutableListOf<BrowserInfo>()

        val activities = queryViewHandlers(context, intent)

        for (resolveInfo in activities) {
            val packageName = resolveInfo.activityInfo.packageName
            if (packageName == selfPackage || !seen.add(packageName)) continue
            val label = resolveInfo.loadLabel(context.packageManager).toString()
            browsers.add(BrowserInfo(label = label, packageName = packageName))
        }

        return browsers.sortedBy { it.label.lowercase() }
    }

    fun open(context: Context, url: String, targetPackage: String): Boolean {
        val uri = Uri.parse(url)
        val packageToUse = targetPackage.ifBlank {
            installedBrowsers(context).firstOrNull()?.packageName.orEmpty()
        }

        if (packageToUse.isBlank()) {
            return false
        }

        val intent = Intent(Intent.ACTION_VIEW, uri).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            setPackage(packageToUse)
        }

        if (intent.resolveActivity(context.packageManager) == null) {
            return false
        }

        context.startActivity(intent)
        return true
    }

    @Suppress("DEPRECATION")
    private fun queryViewHandlers(context: Context, intent: Intent) =
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.TIRAMISU) {
            context.packageManager.queryIntentActivities(
                intent,
                PackageManager.ResolveInfoFlags.of(PackageManager.MATCH_ALL.toLong())
            )
        } else {
            context.packageManager.queryIntentActivities(intent, PackageManager.MATCH_ALL)
        }
}
