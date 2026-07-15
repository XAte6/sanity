package com.sanity.urlcleaner

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone
import java.util.regex.Pattern
import java.util.regex.PatternSyntaxException

object AppConfigStore {
    private const val FILE_NAME = "config.json"

    fun load(context: Context): AppConfig {
        val file = configFile(context)
        if (!file.exists()) {
            val defaults = createDefault(context)
            save(context, defaults)
            return defaults
        }

        return try {
            parseConfig(context, JSONObject(file.readText()))
        } catch (_: Exception) {
            val defaults = createDefault(context)
            save(context, defaults)
            defaults
        }
    }

    fun save(context: Context, config: AppConfig) {
        configFile(context).writeText(toJsonString(config))
    }

    fun toJsonString(config: AppConfig): String = configToJson(config).toString(2)

    fun loadFromJsonText(context: Context, text: String): AppConfig {
        val config = parseConfig(context, JSONObject(text.trim()))
        validateRules(config.rules)
        save(context, config)
        return config
    }

    fun createDefault(context: Context): AppConfig {
        val catalog = DefaultRules.loadLocal(context)
        return AppConfig(rulesVersion = catalog.version, rules = catalog.rules)
    }

    fun parseSleepDate(value: String): Long? {
        return try {
            val formatter = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US)
            formatter.timeZone = TimeZone.getTimeZone("UTC")
            formatter.parse(value)?.time
        } catch (_: Exception) {
            try {
                val formatter = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.US)
                formatter.timeZone = TimeZone.getTimeZone("UTC")
                formatter.parse(value)?.time
            } catch (_: Exception) {
                null
            }
        }
    }

    private fun configFile(context: Context): File = File(context.filesDir, FILE_NAME)

    private fun parseConfig(context: Context, json: JSONObject): AppConfig {
        val rules = parseRulesFromJson(json.optJSONArray("rules"))
        val catalogFallback = if (rules.isEmpty()) DefaultRules.loadLocal(context) else null
        return AppConfig(
            enabled = json.optBoolean("enabled", true),
            linkProxyEnabled = json.optBoolean("linkProxyEnabled", true),
            targetBrowser = json.optString("targetBrowser", ""),
            notificationsEnabled = json.optBoolean("notificationsEnabled", true),
            updatesEnabled = if (json.has("updatesEnabled")) {
                json.optBoolean("updatesEnabled", true)
            } else {
                true
            },
            sleepUntil = if (json.has("sleepUntil") && !json.isNull("sleepUntil")) {
                json.getString("sleepUntil")
            } else {
                null
            },
            rulesVersion = if (json.has("rulesVersion")) {
                json.optInt("rulesVersion", 1)
            } else {
                1
            },
            lastUpdateCheck = if (json.has("lastUpdateCheck") && !json.isNull("lastUpdateCheck")) {
                json.getString("lastUpdateCheck")
            } else {
                null
            },
            rules = catalogFallback?.rules ?: rules
        ).let { config ->
            if (catalogFallback != null && !json.has("rulesVersion")) {
                config.copy(rulesVersion = catalogFallback.version)
            } else {
                config
            }
        }
    }

    private fun configToJson(config: AppConfig): JSONObject {
        return JSONObject().apply {
            put("enabled", config.enabled)
            put("linkProxyEnabled", config.linkProxyEnabled)
            put("targetBrowser", config.targetBrowser)
            put("notificationsEnabled", config.notificationsEnabled)
            put("updatesEnabled", config.updatesEnabled)
            if (config.sleepUntil != null) put("sleepUntil", config.sleepUntil)
            put("rulesVersion", config.rulesVersion)
            if (config.lastUpdateCheck != null) put("lastUpdateCheck", config.lastUpdateCheck)
            put("rules", JSONArray().apply {
                config.rules.forEach { rule ->
                    put(JSONObject().apply {
                        put("domain", rule.domain)
                        put("regex", rule.regex)
                    })
                }
            })
        }
    }

    private fun parseRulesFromJson(array: JSONArray?): List<UrlRule> {
        if (array == null || array.length() == 0) return emptyList()

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
        return rules
    }

    private fun validateRules(rules: List<UrlRule>) {
        if (rules.isEmpty()) {
            throw IllegalArgumentException("At least one rule is required.")
        }

        for ((index, rule) in rules.withIndex()) {
            if (rule.domain.isBlank()) {
                throw IllegalArgumentException("Rule ${index + 1} is missing a domain.")
            }
            if (rule.regex.isBlank()) {
                throw IllegalArgumentException("Rule ${index + 1} is missing a regex.")
            }
            try {
                Pattern.compile(rule.regex)
            } catch (e: PatternSyntaxException) {
                throw IllegalArgumentException("Rule ${index + 1} has invalid regex: ${e.message}")
            }
        }
    }
}
