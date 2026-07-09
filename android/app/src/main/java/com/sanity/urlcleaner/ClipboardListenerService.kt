package com.sanity.urlcleaner

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.os.IBinder

class ClipboardListenerService : Service() {

    private lateinit var clipboardManager: ClipboardManager
    private val clipListener = ClipboardManager.OnPrimaryClipChangedListener { onClipChanged() }

    override fun onCreate() {
        super.onCreate()
        clipboardManager = getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        clipboardManager.addPrimaryClipChangedListener(clipListener)
        startForeground(NOTIFICATION_ID, buildNotification())
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int = START_STICKY

    override fun onDestroy() {
        super.onDestroy()
        clipboardManager.removePrimaryClipChangedListener(clipListener)
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun onClipChanged() {
        val config = AppConfigStore.load(this)
        if (!config.isActive) return

        val clip = clipboardManager.primaryClip ?: return
        if (clip.itemCount == 0) return
        val text = clip.getItemAt(0).coerceToText(this).toString()

        val url = UrlCleaner.extractHttpUrl(text) ?: return
        val cleaned = UrlCleaner.tryClean(url, config.rules) ?: return

        clipboardManager.setPrimaryClip(ClipData.newPlainText("url", cleaned))

        if (config.notificationsEnabled) {
            showCleanedNotification()
        }
    }

    private fun showCleanedNotification() {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val notification = Notification.Builder(this, CHANNEL_TOAST_ID)
            .setSmallIcon(android.R.drawable.ic_menu_close_clear_cancel)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(getString(R.string.clipboard_cleaned))
            .setAutoCancel(true)
            .build()
        nm.notify(NOTIFICATION_TOAST_ID, notification)
    }

    private fun buildNotification(): Notification {
        ensureChannels()
        val openIntent = PendingIntent.getActivity(
            this, 0,
            Intent(this, SettingsActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE
        )
        return Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_menu_close_clear_cancel)
            .setContentTitle(getString(R.string.app_name))
            .setContentText(getString(R.string.clipboard_listener_running))
            .setOngoing(true)
            .setContentIntent(openIntent)
            .build()
    }

    private fun ensureChannels() {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        if (nm.getNotificationChannel(CHANNEL_ID) == null) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                getString(R.string.clipboard_channel_name),
                NotificationManager.IMPORTANCE_MIN
            ).apply {
                setShowBadge(false)
                lockscreenVisibility = Notification.VISIBILITY_SECRET
            }
            nm.createNotificationChannel(channel)
        }
        if (nm.getNotificationChannel(CHANNEL_TOAST_ID) == null) {
            val channel = NotificationChannel(
                CHANNEL_TOAST_ID,
                getString(R.string.clipboard_cleaned_channel_name),
                NotificationManager.IMPORTANCE_LOW
            )
            nm.createNotificationChannel(channel)
        }
    }

    companion object {
        private const val CHANNEL_ID = "sanity_clipboard"
        private const val CHANNEL_TOAST_ID = "sanity_clipboard_cleaned"
        private const val NOTIFICATION_ID = 1
        private const val NOTIFICATION_TOAST_ID = 2

        fun start(context: Context) {
            context.startForegroundService(Intent(context, ClipboardListenerService::class.java))
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, ClipboardListenerService::class.java))
        }
    }
}
