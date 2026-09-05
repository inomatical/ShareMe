package com.inomatical.bearshare

import android.content.Context

internal class FailedOutgoingPreferences(context: Context) {
    private val preferences = context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)

    fun load(): FailedOutgoingItem? {
        val kind = preferences.getString(KIND_KEY, null) ?: return null
        val content = preferences.getString(CONTENT_KEY, null) ?: return null
        return FailedOutgoingItem(kind, content)
    }

    fun save(item: FailedOutgoingItem) {
        preferences.edit()
            .putString(KIND_KEY, item.kind)
            .putString(CONTENT_KEY, item.content)
            .apply()
    }

    fun discard() {
        preferences.edit().clear().apply()
    }

    private companion object {
        const val PREFERENCES_NAME = "bearshare_failed_outgoing"
        const val KIND_KEY = "kind"
        const val CONTENT_KEY = "content"
    }
}