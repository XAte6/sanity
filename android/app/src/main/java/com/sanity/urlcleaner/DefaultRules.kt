package com.sanity.urlcleaner

import android.content.Context
import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL

data class RegexRulesCatalog(
    val version: Int,
    val rules: List<UrlRule>
)

object DefaultRules {
    private const val ASSET_NAME = "regex-rules.json"

    fun create(context: Context): List<UrlRule> = loadLocal(context).rules

    fun loadLocal(context: Context): RegexRulesCatalog {
        context.assets.open(ASSET_NAME).use { stream ->
            val text = BufferedReader(InputStreamReader(stream, Charsets.UTF_8)).readText()
            return parse(text)
        }
    }

    fun fetchRemote(): RegexRulesCatalog {
        val connection = (URL(AppLinks.REGEX_RULES_RAW).openConnection() as HttpURLConnection).apply {
            connectTimeout = 15000
            readTimeout = 15000
            requestMethod = "GET"
            setRequestProperty("User-Agent", "Sanity")
        }
        try {
            val code = connection.responseCode
            if (code !in 200..299) {
                throw IllegalStateException("HTTP $code fetching regex rules")
            }
            val text = connection.inputStream.bufferedReader(Charsets.UTF_8).readText()
            return parse(text)
        } finally {
            connection.disconnect()
        }
    }

    fun loadForReset(context: Context): RegexRulesCatalog {
        return try {
            fetchRemote()
        } catch (_: Exception) {
            loadLocal(context)
        }
    }

    fun parse(text: String): RegexRulesCatalog {
        val json = JSONObject(text)
        val version = json.optInt("version", 1)
        val array = json.getJSONArray("rules")
        val rules = mutableListOf<UrlRule>()
        for (i in 0 until array.length()) {
            val item = array.getJSONObject(i)
            rules.add(
                UrlRule(
                    domain = item.optString("domain", "*"),
                    regex = item.optString("regex", "")
                )
            )
        }
        require(rules.isNotEmpty()) { "Regex rules catalog is empty." }
        return RegexRulesCatalog(version = version.coerceAtLeast(1), rules = rules)
    }
}
