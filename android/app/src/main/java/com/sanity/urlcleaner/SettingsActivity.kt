package com.sanity.urlcleaner

import android.content.Intent
import android.os.Bundle
import android.provider.Settings
import android.widget.ArrayAdapter
import androidx.appcompat.app.AppCompatActivity
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
        browsers = BrowserForwarder.installedBrowsers(this)

        if (config.targetBrowser.isBlank()) {
            config = config.copy(targetBrowser = browsers.firstOrNull()?.packageName ?: "")
            AppConfigStore.save(this, config)
        }

        bindUi()
    }

    override fun onResume() {
        super.onResume()
        binding.statusText.text = if (isSanityDefaultBrowser()) {
            getString(R.string.status_default_browser)
        } else {
            getString(R.string.status_not_default_browser)
        }
    }

    private fun bindUi() {
        binding.enabledSwitch.isChecked = config.enabled
        binding.linkProxySwitch.isChecked = config.linkProxyEnabled
        binding.notificationsSwitch.isChecked = config.notificationsEnabled

        val labels = browsers.map { it.label }
        binding.browserSpinner.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_dropdown_item, labels)
        val selectedIndex = browsers.indexOfFirst { it.packageName == config.targetBrowser }.coerceAtLeast(0)
        binding.browserSpinner.setSelection(selectedIndex)

        binding.enabledSwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(enabled = checked)
            save()
        }

        binding.linkProxySwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(linkProxyEnabled = checked)
            save()
        }

        binding.notificationsSwitch.setOnCheckedChangeListener { _, checked ->
            config = config.copy(notificationsEnabled = checked)
            save()
        }

        binding.browserSpinner.setOnItemSelectedListener(SimpleItemSelectedListener {
            val browser = browsers.getOrNull(binding.browserSpinner.selectedItemPosition) ?: return@SimpleItemSelectedListener
            config = config.copy(targetBrowser = browser.packageName)
            save()
        })

        binding.defaultBrowserButton.setOnClickListener {
            startActivity(Intent(Settings.ACTION_MANAGE_DEFAULT_APPS_SETTINGS))
        }

        binding.setupHelp.text = getString(R.string.setup_help)
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
