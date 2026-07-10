package com.sanity.urlcleaner

data class UrlRule(
    val domain: String,
    val regex: String
)

data class AppConfig(
    val enabled: Boolean = true,
    val linkProxyEnabled: Boolean = true,
    val targetBrowser: String = "",
    val notificationsEnabled: Boolean = true,
    val sleepUntil: String? = null,
    val rules: List<UrlRule> = DefaultRules.create()
) {
    val isActive: Boolean
        get() {
            if (!enabled) return false
            val until = sleepUntil?.let { AppConfigStore.parseSleepDate(it) }
            if (until != null && until > System.currentTimeMillis()) return false
            return true
        }

    val isLinkProxyActive: Boolean
        get() = isActive
}
