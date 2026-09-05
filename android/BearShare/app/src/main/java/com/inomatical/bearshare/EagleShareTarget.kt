package com.inomatical.bearshare

data class EagleShareTarget(
    val address: String,
    val port: Int,
)

sealed interface TargetValidationResult {
    data class Valid(val target: EagleShareTarget) : TargetValidationResult

    data class Invalid(val addressError: String? = null, val portError: String? = null) : TargetValidationResult
}

object TargetConfiguration {
    fun validate(addressInput: String, portInput: String): TargetValidationResult {
        val address = addressInput.trim()
        val addressError = if (isNumericIpAddress(address)) null else "Enter a valid IPv4 or IPv6 address."
        val port = portInput.trim().toIntOrNull()
        val portError = if (port in 1..65535) null else "Enter a port from 1 to 65535."

        return if (addressError == null && portError == null) {
            TargetValidationResult.Valid(EagleShareTarget(address, port!!))
        } else {
            TargetValidationResult.Invalid(addressError, portError)
        }
    }

    private fun isNumericIpAddress(address: String): Boolean =
        isIpv4Address(address) || isIpv6Address(address)

    private fun isIpv4Address(address: String): Boolean {
        val parts = address.split('.')
        return parts.size == 4 && parts.all { part ->
            part.isNotEmpty() &&
                part.length <= 3 &&
                part.all(Char::isDigit) &&
                part.toIntOrNull() in 0..255
        }
    }

    private fun isIpv6Address(address: String): Boolean {
        if (!address.contains(':') || address.contains(":::") || address.any { character -> !character.isDigit() && character.lowercaseChar() !in 'a'..'f' && character != ':' }) {
            return false
        }

        val sections = address.split("::")
        if (sections.size > 2) {
            return false
        }

        val explicitSections = address.split(':').filter { it.isNotEmpty() }
        if (explicitSections.any { section -> section.length > 4 }) {
            return false
        }

        val explicitSectionCount = explicitSections.size
        return when {
            sections.size == 1 -> explicitSectionCount == 8
            else -> explicitSectionCount < 8
        }
    }
}