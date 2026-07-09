package com.sanity.urlcleaner

import android.content.Intent
import android.os.Bundle
import android.provider.Settings
import android.view.View
import android.widget.ArrayAdapter
import androidx.activity.OnBackPressedCallback
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import com.sanity.urlcleaner.databinding.ActivitySettingsBinding

class SettingsActivity : AppCompatActivity() {
    private lateinit var binding: ActivitySettingsBinding
    private lateinit var config: AppConfig
    private var browsers: List<BrowserInfo> = emptyList()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        config = AppConfigStore.load(this)
        refreshBrowsers()
        bindUi()

        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                attemptExit()
            }
        })
    }

    override fun onResume() {
        super.onResume()
        refreshBrowsers()
        updateBrowserSpinner()
        updateChecklist()
    }

    private fun refreshBrowsers() {
        browsers = BrowserForwarder.installedBrowsers(this)
        if (config.targetBrowser.isBlank() || browsers.none { it.packageName == config.targetBrowser }) {
            config = config.copy(targetBrowser = browsers.firstOrNull()?.packageName ?: "")
            AppConfigStore.save(this, config)
        }
        binding.noBrowsersWarning.visibility = if (browsers.isEmpty()) View.VISIBLE else View.GONE
    }

    private fun bindUi() {
        binding.enabledSwitch.isChecked = config.enabled
        binding.linkProxySwitch.isChecked = config.linkProxyEnabled
        binding.notificationsSwitch.isChecked = config.notificationsEnabled

        updateBrowserSpinner()
        updateChecklist()

        binding.enabledSwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(enabled = checked)
            save()
            updateChecklist()
        }

        binding.linkProxySwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(linkProxyEnabled = checked)
            save()
            updateChecklist()
        }

        binding.notificationsSwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(notificationsEnabled = checked)
            save()
        }

        binding.browserSpinner.setOnItemSelectedListener(SimpleItemSelectedListener {
            val browser = browsers.getOrNull(binding.browserSpinner.selectedItemPosition) ?: return@SimpleItemSelectedListener
            config = config.copy(targetBrowser = browser.packageName)
            save()
            updateChecklist()
        })

        binding.defaultBrowserButton.setOnClickListener {
            startActivity(Intent(Settings.ACTION_MANAGE_DEFAULT_APPS_SETTINGS))
        }

        binding.closeButton.setOnClickListener {
            attemptExit()
        }
    }

    private fun updateBrowserSpinner() {
        val labels = browsers.map { it.label }
        binding.browserSpinner.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_dropdown_item, labels)
        if (labels.isNotEmpty()) {
            val selectedIndex = browsers.indexOfFirst { it.packageName == config.targetBrowser }.coerceAtLeast(0)
            binding.browserSpinner.setSelection(selectedIndex)
        }
    }

    private fun updateChecklist() {
        val defaultBrowserOk = isSanityDefaultBrowser()
        setChecklistLine(
            binding.checklistDefaultBrowser,
            defaultBrowserOk,
            getString(R.string.checklist_default_browser_ok),
            getString(R.string.checklist_default_browser_fail)
        )

        val targetBrowser = browsers.find { it.packageName == config.targetBrowser }
        val targetBrowserOk = targetBrowser != null
        setChecklistLine(
            binding.checklistTargetBrowser,
            targetBrowserOk,
            getString(R.string.checklist_target_browser_ok, targetBrowser?.label ?: ""),
            getString(R.string.checklist_target_browser_fail)
        )

        setChecklistLine(
            binding.checklistEnabled,
            config.enabled,
            getString(R.string.checklist_enabled_ok),
            getString(R.string.checklist_enabled_fail)
        )

        setChecklistLine(
            binding.checklistLinkProxy,
            config.linkProxyEnabled,
            getString(R.string.checklist_link_proxy_ok),
            getString(R.string.checklist_link_proxy_fail)
        )
    }

    private fun setChecklistLine(view: android.widget.TextView, ok: Boolean, okText: String, failText: String) {
        val mark = getString(if (ok) R.string.check_ok else R.string.check_fail)
        val color = ContextCompat.getColor(this, if (ok) R.color.sanity_green else R.color.sanity_red)
        view.text = "$mark ${if (ok) okText else failText}"
        view.setTextColor(color)
    }

    private fun isFullyConfigured(): Boolean {
        val targetBrowserOk = browsers.any { it.packageName == config.targetBrowser }
        return isSanityDefaultBrowser() && targetBrowserOk && config.enabled && config.linkProxyEnabled
    }

    private fun attemptExit() {
        if (isFullyConfigured()) {
            finish()
            return
        }

        AlertDialog.Builder(this)
            .setTitle(R.string.exit_incomplete_title)
            .setMessage(R.string.exit_incomplete_message)
            .setNegativeButton(R.string.stay, null)
            .setPositiveButton(R.string.exit_anyway) { _, _ -> finish() }
            .show()
    }

    private fun save() {
        AppConfigStore.save(this, config)
    }

    private fun isSanityDefaultBrowser(): Boolean {
        val intent = Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://example.com"))
        val resolveInfo = packageManager.resolveActivity(intent, 0) ?: return false
        return resolveInfo.activityInfo.packageName == packageName
    }
}
