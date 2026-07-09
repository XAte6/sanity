package com.sanity.urlcleaner

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone

object AppConfigStore {
    private const val FILE_NAME = "config.json"

    fun load(context: Context): AppConfig {
        val file = configFile(context)
        if (!file.exists()) {
            val defaults = AppConfig()
            save(context, defaults)
            return defaults
        }

        return try {
            val json = JSONObject(file.readText())
            AppConfig(
                enabled = json.optBoolean("enabled", true),
                linkProxyEnabled = json.optBoolean("linkProxyEnabled", false),
                targetBrowser = json.optString("targetBrowser", ""),
                notificationsEnabled = json.optBoolean("notificationsEnabled", true),
                sleepUntil = if (json.has("sleepUntil") && !json.isNull("sleepUntil")) {
                    json.getString("sleepUntil")
                } else {
                    null
                },
                rules = parseRules(json.optJSONArray("rules"))
            )
        } catch (_: Exception) {
            val defaults = AppConfig()
            save(context, defaults)
            defaults
        }
    }

    fun save(context: Context, config: AppConfig) {
        val json = JSONObject().apply {
            put("enabled", config.enabled)
            put("linkProxyEnabled", config.linkProxyEnabled)
            put("targetBrowser", config.targetBrowser)
            put("notificationsEnabled", config.notificationsEnabled)
            if (config.sleepUntil != null) put("sleepUntil", config.sleepUntil)
            put("rules", JSONArray().apply {
                config.rules.forEach { rule ->
                    put(JSONObject().apply {
                        put("domain", rule.domain)
                        put("regex", rule.regex)
                    })
                }
            })
        }
        configFile(context).writeText(json.toString(2))
    }

    fun parseSleepDate(value: String): Long? {
        return try {
            val formatter = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US)
            formatter.timeZone = TimeZone.getTimeZone("UTC")
            formatter.parse(value)?.time
        } catch (_: Exception) {
            null
        }
    }

    private fun configFile(context: Context): File {
        return File(context.filesDir, FILE_NAME)
    }

    private fun parseRules(array: JSONArray?): List<UrlRule> {
        if (array == null || array.length() == 0) return DefaultRules.create()
        val rules = mutableListOf<UrlRule>()
        for (i in 0 until array.length()) {
            val item = array.optJSONObject(i) ?: continue
            rules.add(
                UrlRule(
                    domain = item.optString("domain", "*"),
                    regex = item.optString("regex", "")
                )
            )
        }
        return if (rules.isEmpty()) DefaultRules.create() else rules
    }
}
