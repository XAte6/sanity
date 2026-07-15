package com.sanity.urlcleaner

import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageButton
import android.widget.TextView
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.widget.addTextChangedListener
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.textfield.TextInputEditText
import com.sanity.urlcleaner.databinding.ActivityRulesBinding

class RulesActivity : AppCompatActivity() {
    private lateinit var binding: ActivityRulesBinding
    private lateinit var config: AppConfig
    private val rules = mutableListOf<UrlRule>()
    private lateinit var adapter: RuleAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityRulesBinding.inflate(layoutInflater)
        setContentView(binding.root)

        config = AppConfigStore.load(this)
        rules.clear()
        rules.addAll(config.rules)

        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.addRuleButton.setOnClickListener { showRuleDialog(null, -1) }
        binding.resetRulesButton.setOnClickListener { confirmReset() }

        adapter = RuleAdapter(
            onEdit = { index -> showRuleDialog(rules[index], index) },
            onDelete = { index -> confirmDelete(index) }
        )
        binding.rulesList.layoutManager = LinearLayoutManager(this)
        binding.rulesList.adapter = adapter

        val filterWatcher = object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) = refreshList()
        }
        binding.domainFilter.addTextChangedListener(filterWatcher)
        binding.regexFilter.addTextChangedListener(filterWatcher)

        refreshList()
    }

    private fun refreshList() {
        val domainFilter = binding.domainFilter.text?.toString()?.trim().orEmpty()
        val regexFilter = binding.regexFilter.text?.toString()?.trim().orEmpty()

        val visible = rules.mapIndexed { index, rule -> index to rule }
            .filter { (_, rule) ->
                (domainFilter.isEmpty() || rule.domain.contains(domainFilter, ignoreCase = true)) &&
                    (regexFilter.isEmpty() || rule.regex.contains(regexFilter, ignoreCase = true))
            }

        adapter.submit(visible)
        binding.rulesCount.text = if (visible.size == rules.size) {
            resources.getQuantityString(R.plurals.rules_count, rules.size, rules.size)
        } else {
            getString(R.string.rules_showing, visible.size, rules.size)
        }
    }

    private fun showRuleDialog(existing: UrlRule?, index: Int) {
        val view = layoutInflater.inflate(R.layout.dialog_rule_edit, null)
        val domainInput = view.findViewById<TextInputEditText>(R.id.domainInput)
        val regexInput = view.findViewById<TextInputEditText>(R.id.regexInput)
        val testUrlInput = view.findViewById<TextInputEditText>(R.id.testUrlInput)
        val testResult = view.findViewById<TextView>(R.id.testResult)

        domainInput.setText(existing?.domain ?: "*")
        regexInput.setText(existing?.regex.orEmpty())

        fun updatePreview() {
            val url = testUrlInput.text?.toString()?.trim().orEmpty()
            if (url.isEmpty()) {
                testResult.setText(R.string.test_result_hint)
                testResult.setTextColor(getColor(R.color.sanity_muted))
                return
            }

            val rule = UrlRule(
                domain = domainInput.text?.toString()?.trim().orEmpty(),
                regex = regexInput.text?.toString()?.trim().orEmpty()
            )
            val cleaned = UrlCleaner.tryClean(url, listOf(rule))
            when {
                cleaned != null -> {
                    testResult.text = cleaned
                    testResult.setTextColor(getColor(R.color.sanity_green))
                }
                !url.startsWith("http://", ignoreCase = true) &&
                    !url.startsWith("https://", ignoreCase = true) -> {
                    testResult.setText(R.string.test_result_invalid_url)
                    testResult.setTextColor(getColor(R.color.sanity_red))
                }
                else -> {
                    testResult.setText(R.string.test_result_no_change)
                    testResult.setTextColor(getColor(R.color.sanity_muted))
                }
            }
        }

        domainInput.addTextChangedListener { updatePreview() }
        regexInput.addTextChangedListener { updatePreview() }
        testUrlInput.addTextChangedListener { updatePreview() }

        AlertDialog.Builder(this)
            .setTitle(if (existing == null) R.string.add_rule else R.string.edit_rule)
            .setView(view)
            .setNegativeButton(R.string.cancel, null)
            .setPositiveButton(R.string.save) { _, _ ->
                val domain = domainInput.text?.toString()?.trim().orEmpty()
                val regex = regexInput.text?.toString()?.trim().orEmpty()
                if (domain.isBlank() && regex.isBlank()) return@setPositiveButton

                val rule = UrlRule(domain = domain.ifBlank { "*" }, regex = regex)
                if (index >= 0) {
                    rules[index] = rule
                } else {
                    rules.add(rule)
                }
                persist()
                refreshList()
            }
            .show()
    }

    private fun confirmDelete(index: Int) {
        val rule = rules[index]
        val label = rule.domain.ifBlank { getString(R.string.blank_domain) }
        AlertDialog.Builder(this)
            .setTitle(R.string.delete_rule)
            .setMessage(getString(R.string.delete_rule_message, label))
            .setNegativeButton(R.string.cancel, null)
            .setPositiveButton(R.string.delete) { _, _ ->
                rules.removeAt(index)
                persist()
                refreshList()
            }
            .show()
    }

    private fun confirmReset() {
        AlertDialog.Builder(this)
            .setTitle(R.string.reset_rules_title)
            .setMessage(R.string.reset_rules_message)
            .setNegativeButton(R.string.cancel, null)
            .setPositiveButton(R.string.reset_to_defaults) { _, _ ->
                try {
                    val catalog = DefaultRules.loadForReset(this)
                    rules.clear()
                    rules.addAll(catalog.rules)
                    config = config.copy(rules = catalog.rules, rulesVersion = catalog.version)
                    AppConfigStore.save(this, config)
                    refreshList()
                } catch (ex: Exception) {
                    AlertDialog.Builder(this)
                        .setTitle(R.string.reset_rules_title)
                        .setMessage(getString(R.string.reset_rules_failed, ex.message ?: ""))
                        .setPositiveButton(android.R.string.ok, null)
                        .show()
                }
            }
            .show()
    }

    private fun persist() {
        val cleaned = rules.filter {
            it.domain.isNotBlank() || it.regex.isNotBlank()
        }
        val fallback = if (cleaned.isEmpty()) DefaultRules.loadLocal(this) else null
        config = config.copy(
            rules = fallback?.rules ?: cleaned,
            rulesVersion = fallback?.version ?: config.rulesVersion
        )
        AppConfigStore.save(this, config)
        rules.clear()
        rules.addAll(config.rules)
    }
}

private class RuleAdapter(
    private val onEdit: (Int) -> Unit,
    private val onDelete: (Int) -> Unit
) : RecyclerView.Adapter<RuleAdapter.Holder>() {
    private val items = mutableListOf<Pair<Int, UrlRule>>()

    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val domain: TextView = view.findViewById(R.id.ruleDomain)
        val regex: TextView = view.findViewById(R.id.ruleRegex)
        val edit: ImageButton = view.findViewById(R.id.editButton)
        val delete: ImageButton = view.findViewById(R.id.deleteButton)
    }

    fun submit(values: List<Pair<Int, UrlRule>>) {
        items.clear()
        items.addAll(values)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_rule, parent, false)
        return Holder(view)
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val (index, rule) = items[position]
        holder.domain.text = rule.domain
        holder.regex.text = rule.regex
        holder.edit.setOnClickListener { onEdit(index) }
        holder.delete.setOnClickListener { onDelete(index) }
    }

    override fun getItemCount(): Int = items.size
}
