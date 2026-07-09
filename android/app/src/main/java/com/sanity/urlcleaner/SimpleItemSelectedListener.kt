package com.sanity.urlcleaner

import android.view.View
import android.widget.AdapterView

class SimpleItemSelectedListener(
    private val onSelected: () -> Unit
) : AdapterView.OnItemSelectedListener {
    private var first = true

    override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
        if (first) {
            first = false
            return
        }
        onSelected()
    }

    override fun onNothingSelected(parent: AdapterView<*>?) = Unit
}
