package com.sanity.urlcleaner

import android.content.Context
import android.net.Uri
import org.json.JSONObject
import java.io.File

data class UsageMetrics(
    val linksCleaned: Int = 0,
    val domains: Map<String, Int> = emptyMap()
) {
    val domainCount: Int get() = domains.size

    fun summaryText(): String {
        val linkWord = if (linksCleaned == 1) "link" else "links"
        val domainWord = if (domainCount == 1) "domain" else "domains"
        return "$linksCleaned $linkWord cleaned across $domainCount $domainWord"
    }
}

object UsageMetricsStore {
    private const val FILE_NAME = "metrics.json"

    fun load(context: Context): UsageMetrics {
        val file = file(context)
        if (!file.exists()) return UsageMetrics()

        return try {
            val json = JSONObject(file.readText())
            val domainsJson = json.optJSONObject("domains") ?: JSONObject()
            val domains = mutableMapOf<String, Int>()
            val keys = domainsJson.keys()
            while (keys.hasNext()) {
                val key = keys.next()
                domains[key] = domainsJson.optInt(key, 0)
            }
            UsageMetrics(
                linksCleaned = json.optInt("linksCleaned", 0),
                domains = domains
            )
        } catch (_: Exception) {
            UsageMetrics()
        }
    }

    fun recordClean(context: Context, url: String) {
        val host = extractHost(url) ?: return

        repeat(5) {
            try {
                val current = load(context)
                val domains = current.domains.toMutableMap()
                domains[host] = (domains[host] ?: 0) + 1
                save(
                    context,
                    UsageMetrics(
                        linksCleaned = current.linksCleaned + 1,
                        domains = domains
                    )
                )
                return
            } catch (_: Exception) {
                Thread.sleep(40)
            }
        }
    }

    private fun save(context: Context, metrics: UsageMetrics) {
        val domainsJson = JSONObject()
        for ((domain, count) in metrics.domains) {
            domainsJson.put(domain, count)
        }
        val json = JSONObject()
            .put("linksCleaned", metrics.linksCleaned)
            .put("domains", domainsJson)

        val target = file(context)
        val temp = File(target.parentFile, "$FILE_NAME.tmp")
        temp.writeText(json.toString(2))
        if (target.exists()) {
            target.delete()
        }
        temp.renameTo(target)
    }

    private fun file(context: Context): File =
        File(context.filesDir, FILE_NAME)

    private fun extractHost(url: String): String? {
        val host = Uri.parse(url.trim()).host ?: return null
        return host.lowercase().takeIf { it.isNotEmpty() }
    }
}
