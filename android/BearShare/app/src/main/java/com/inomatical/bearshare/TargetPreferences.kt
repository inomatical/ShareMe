package com.inomatical.bearshare

import android.content.Context

class TargetPreferences(context: Context) {
    private val preferences = context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)

    fun load(): EagleShareTarget? {
        val address = preferences.getString(ADDRESS_KEY, null) ?: return null
        val port = preferences.getInt(PORT_KEY, INVALID_PORT)

        return when (val validation = TargetConfiguration.validate(address, port.toString())) {
            is TargetValidationResult.Valid -> validation.target
            is TargetValidationResult.Invalid -> null
        }
    }

    fun save(target: EagleShareTarget) {
        preferences.edit()
            .putString(ADDRESS_KEY, target.address)
            .putInt(PORT_KEY, target.port)
            .apply()
    }

    private companion object {
        const val PREFERENCES_NAME = "bearshare_target"
        const val ADDRESS_KEY = "address"
        const val PORT_KEY = "port"
        const val INVALID_PORT = -1
    }
}