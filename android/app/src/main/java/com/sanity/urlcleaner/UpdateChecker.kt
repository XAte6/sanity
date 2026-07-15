package com.sanity.urlcleaner

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.appcompat.app.AlertDialog
import org.json.JSONArray
import java.net.HttpURLConnection
import java.net.URL
import java.text.SimpleDateFormat
import java.time.Instant
import java.util.Date
import java.util.Locale
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean

object UpdateChecker {
    private const val CHECK_INTERVAL_MS = 7L * 24 * 60 * 60 * 1000
    private val running = AtomicBoolean(false)

    /**
     * Always invokes [onFinished] on the UI thread (even when skipped),
     * so short-lived activities can finish after the check.
     */
    fun runAsync(activity: Activity, config: AppConfig, onFinished: (AppConfig) -> Unit) {
        if (!config.updatesEnabled || !isDue(config) || !running.compareAndSet(false, true)) {
            onFinished(config)
            return
        }

        val appContext = activity.applicationContext
        Executors.newSingleThreadExecutor().execute {
            var remoteRules: RegexRulesCatalog? = null
            var remoteReleaseDate: Date? = null

            try {
                remoteRules = DefaultRules.fetchRemote()
            } catch (_: Exception) {
            }

            try {
                remoteReleaseDate = fetchReleaseFileDate()
            } catch (_: Exception) {
            }

            activity.runOnUiThread {
                if (activity.isFinishing) {
                    running.set(false)
                    onFinished(config)
                    return@runOnUiThread
                }
                askRegexThenBuild(
                    activity = activity,
                    appContext = appContext,
                    config = config,
                    remoteRules = remoteRules,
                    remoteReleaseDate = remoteReleaseDate,
                    onFinished = { updated ->
                        running.set(false)
                        onFinished(updated)
                    }
                )
            }
        }
    }

    private fun isDue(config: AppConfig): Boolean {
        val last = config.lastUpdateCheck?.let { AppConfigStore.parseSleepDate(it) } ?: return true
        return System.currentTimeMillis() - last >= CHECK_INTERVAL_MS
    }

    private fun askRegexThenBuild(
        activity: Activity,
        appContext: Context,
        config: AppConfig,
        remoteRules: RegexRulesCatalog?,
        remoteReleaseDate: Date?,
        onFinished: (AppConfig) -> Unit
    ) {
        fun finish(updated: AppConfig) {
            val stamped = updated.copy(lastUpdateCheck = encodeUtcNow())
            AppConfigStore.save(appContext, stamped)
            onFinished(stamped)
        }

        if (remoteRules != null && remoteRules.version > config.rulesVersion) {
            AlertDialog.Builder(activity)
                .setTitle("Regex update")
                .setMessage(
                    "A newer regex list is available (v${remoteRules.version}). " +
                        "Replace your current rules with the updated defaults?"
                )
                .setNegativeButton("Not now") { _, _ ->
                    askBuildUpdate(activity, config, remoteReleaseDate, ::finish)
                }
                .setPositiveButton("Update") { _, _ ->
                    val next = config.copy(
                        rules = remoteRules.rules,
                        rulesVersion = remoteRules.version
                    )
                    askBuildUpdate(activity, next, remoteReleaseDate, ::finish)
                }
                .setOnCancelListener {
                    askBuildUpdate(activity, config, remoteReleaseDate, ::finish)
                }
                .show()
            return
        }

        askBuildUpdate(activity, config, remoteReleaseDate, ::finish)
    }

    private fun askBuildUpdate(
        activity: Activity,
        config: AppConfig,
        remoteReleaseDate: Date?,
        finish: (AppConfig) -> Unit
    ) {
        val localDate = packageDate(activity)
        if (remoteReleaseDate != null && localDate != null &&
            remoteReleaseDate.time > localDate.time + 60_000
        ) {
            val dated = SimpleDateFormat("d MMM yyyy", Locale.getDefault()).format(remoteReleaseDate)
            AlertDialog.Builder(activity)
                .setTitle("App update")
                .setMessage(
                    "A newer Sanity build is available on GitHub (release file dated $dated). " +
                        "Open the download page?"
                )
                .setNegativeButton("Not now") { _, _ -> finish(config) }
                .setPositiveButton("Open") { _, _ ->
                    activity.startActivity(
                        Intent(Intent.ACTION_VIEW, Uri.parse(AppLinks.RELEASE_ASSET))
                    )
                    finish(config)
                }
                .setOnCancelListener { finish(config) }
                .show()
            return
        }

        finish(config)
    }

    private fun encodeUtcNow(): String = Instant.now().toString()

    private fun packageDate(context: Context): Date? {
        return try {
            val info = context.packageManager.getPackageInfo(context.packageName, 0)
            Date(info.lastUpdateTime)
        } catch (_: Exception) {
            null
        }
    }

    private fun fetchReleaseFileDate(): Date {
        val connection = (URL(AppLinks.RELEASE_COMMITS_API).openConnection() as HttpURLConnection).apply {
            connectTimeout = 15000
            readTimeout = 15000
            requestMethod = "GET"
            setRequestProperty("User-Agent", "Sanity")
            setRequestProperty("Accept", "application/vnd.github+json")
        }
        try {
            val text = connection.inputStream.bufferedReader(Charsets.UTF_8).readText()
            val array = JSONArray(text)
            require(array.length() > 0) { "No commits for release file" }
            val dateText = array.getJSONObject(0)
                .getJSONObject("commit")
                .getJSONObject("committer")
                .getString("date")
            return Date.from(Instant.parse(dateText))
        } finally {
            connection.disconnect()
        }
    }
}
