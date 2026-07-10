package com.sanity.urlcleaner

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.sanity.urlcleaner.databinding.ActivityStatisticsBinding
import kotlin.math.roundToInt

class StatisticsActivity : AppCompatActivity() {
    private lateinit var binding: ActivityStatisticsBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityStatisticsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        bindLinks()
        renderMetrics()
    }

    override fun onResume() {
        super.onResume()
        renderMetrics()
    }

    private fun renderMetrics() {
        val metrics = UsageMetricsStore.load(this)
        val rows = metrics.domains.entries
            .sortedWith(compareByDescending<Map.Entry<String, Int>> { it.value }.thenBy { it.key })
        val total = metrics.linksCleaned.coerceAtLeast(1)
        val topShare = rows.firstOrNull()?.let { ((100.0 * it.value) / total).roundToInt() }

        binding.linksCleanedValue.text = "%,d".format(metrics.linksCleaned)
        binding.linksCleanedCaption.text = getString(
            R.string.total_cleaned_caption,
            metrics.linksCleaned,
            resources.getQuantityString(R.plurals.clicks, metrics.linksCleaned)
        )

        if (topShare != null && metrics.linksCleaned > 0) {
            binding.topShareValue.visibility = View.VISIBLE
            binding.topShareValue.text = getString(R.string.percent_value, topShare)
        } else {
            binding.topShareValue.visibility = View.GONE
        }

        binding.domainsValue.text = "%,d".format(metrics.domainCount)
        binding.domainsCaption.text = resources.getQuantityString(
            R.plurals.domains_protected,
            metrics.domainCount,
            metrics.domainCount
        )

        binding.domainsList.layoutManager = LinearLayoutManager(this)
        binding.domainsList.adapter = DomainStatAdapter(rows, metrics.linksCleaned)
    }

    private fun bindLinks() {
        binding.tipFab.tipButton.setOnClickListener { openUrl(AppLinks.TIP) }
    }

    private fun openUrl(url: String) {
        startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(url)))
    }
}

private class DomainStatAdapter(
    private val rows: List<Map.Entry<String, Int>>,
    private val totalCleaned: Int
) : RecyclerView.Adapter<DomainStatAdapter.Holder>() {

    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val name: TextView = view.findViewById(R.id.domainName)
        val count: TextView = view.findViewById(R.id.domainCount)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_domain_stat, parent, false)
        return Holder(view)
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        if (rows.isEmpty()) {
            holder.name.setText(R.string.no_cleans_yet)
            holder.name.setTextColor(holder.itemView.context.getColor(R.color.sanity_muted))
            holder.count.text = "—"
            return
        }

        val row = rows[position]
        val percent = if (totalCleaned <= 0) 0 else ((100.0 * row.value) / totalCleaned).roundToInt()
        holder.name.text = row.key
        holder.name.setTextColor(holder.itemView.context.getColor(R.color.sanity_ink))
        holder.count.text = holder.itemView.context.getString(
            R.string.domain_count_with_percent,
            row.value,
            percent
        )
        holder.name.typeface = if (position < 3) {
            android.graphics.Typeface.DEFAULT_BOLD
        } else {
            android.graphics.Typeface.DEFAULT
        }
    }

    override fun getItemCount(): Int = if (rows.isEmpty()) 1 else rows.size
}
