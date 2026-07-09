package com.sanity.urlcleaner

import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.sanity.urlcleaner.databinding.ActivityConfigEditorBinding

class ConfigEditorActivity : AppCompatActivity() {
    private lateinit var binding: ActivityConfigEditorBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityConfigEditorBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.configEditor.setText(AppConfigStore.toJsonString(AppConfigStore.load(this)))

        binding.saveButton.setOnClickListener { saveConfig() }
        binding.cancelButton.setOnClickListener { finish() }
        binding.resetRulesButton.setOnClickListener { resetRules() }
    }

    private fun saveConfig() {
        try {
            AppConfigStore.loadFromJsonText(this, binding.configEditor.text.toString())
            Toast.makeText(this, R.string.config_saved, Toast.LENGTH_SHORT).show()
            finish()
        } catch (e: Exception) {
            Toast.makeText(this, getString(R.string.config_invalid, e.message ?: ""), Toast.LENGTH_LONG).show()
        }
    }

    private fun resetRules() {
        val config = AppConfigStore.load(this).copy(rules = DefaultRules.create())
        binding.configEditor.setText(AppConfigStore.toJsonString(config))
    }
}
