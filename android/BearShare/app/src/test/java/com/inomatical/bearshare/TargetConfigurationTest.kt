package com.inomatical.bearshare

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class TargetConfigurationTest {
    @Test
    fun acceptsIpv4AndValidPort() {
        val result = TargetConfiguration.validate("192.168.1.44", "61432")

        assertEquals(
            TargetValidationResult.Valid(EagleShareTarget("192.168.1.44", 61432)),
            result,
        )
    }

    @Test
    fun acceptsIpv6AndValidPort() {
        val result = TargetConfiguration.validate("fe80::1", "8080")

        assertTrue(result is TargetValidationResult.Valid)
    }

    @Test
    fun rejectsInvalidAddressAndPortBeforeSaving() {
        val result = TargetConfiguration.validate("eagleshare.local", "70000")

        assertEquals(
            TargetValidationResult.Invalid(
                addressError = "Enter a valid IPv4 or IPv6 address.",
                portError = "Enter a port from 1 to 65535.",
            ),
            result,
        )
    }

    @Test
    fun rejectsMalformedIpv6AddressBeforeSaving() {
        val result = TargetConfiguration.validate("aaaaa::1", "61432")

        assertTrue(result is TargetValidationResult.Invalid)
    }
}