package com.sanity.urlcleaner

import android.net.Uri
import java.util.regex.Pattern

object UrlCleaner {
    private val embeddedUrlPattern =
        Pattern.compile("""https?://[^\s<>"')\]]+""", Pattern.CASE_INSENSITIVE)

    fun extractHttpUrl(text: String): String? {
        val trimmed = text.trim()
        if (trimmed.isEmpty()) return null

        val direct = trimmed.trimEnd(',', '.', ')', ']', '>', '"', '\'')
        if (isHttpUrl(direct)) return direct

        val matcher = embeddedUrlPattern.matcher(trimmed)
        if (!matcher.find()) return null

        val found = matcher.group().trimEnd(',', '.', ')', ']', '>', '"', '\'')
        return if (isHttpUrl(found)) found else null
    }

    fun tryClean(text: String, rules: List<UrlRule>): String? {
        val trimmed = text.trim()
        if (trimmed.isEmpty()) return null
        if (!trimmed.startsWith("http://", ignoreCase = true) &&
            !trimmed.startsWith("https://", ignoreCase = true)) {
            return null
        }

        val uri = Uri.parse(trimmed)
        val scheme = uri.scheme?.lowercase()
        val host = uri.host
        if ((scheme != "http" && scheme != "https") || host.isNullOrEmpty()) return null

        var result = trimmed
        for (rule in rules) {
            if (!domainMatches(host, rule.domain)) continue
            try {
                result = Pattern.compile(rule.regex, Pattern.CASE_INSENSITIVE)
                    .matcher(result)
                    .replaceAll("")
            } catch (_: Exception) {
                // Skip invalid regex patterns in user config.
            }
        }

        result = tidyUrl(result)
        return if (result == trimmed) null else result
    }

    private fun isHttpUrl(text: String): Boolean {
        if (!text.startsWith("http://", ignoreCase = true) &&
            !text.startsWith("https://", ignoreCase = true)) {
            return false
        }

        val uri = Uri.parse(text)
        val scheme = uri.scheme?.lowercase()
        val host = uri.host
        return (scheme == "http" || scheme == "https") && !host.isNullOrEmpty()
    }

    private fun domainMatches(host: String, domain: String): Boolean {
        if (domain.isEmpty() || domain == "*") return true
        val hostLower = host.lowercase()
        val domainLower = domain.lowercase()
        return hostLower == domainLower || hostLower.endsWith(".$domainLower")
    }

    private fun tidyUrl(url: String): String {
        var value = url
        val patterns = listOf("[?&]+$", "\\?&", "&&+")
        val replacements = listOf("", "?", "&")
        for ((pattern, replacement) in patterns.zip(replacements)) {
            value = Pattern.compile(pattern).matcher(value).replaceAll(replacement)
        }
        return value
    }
}
