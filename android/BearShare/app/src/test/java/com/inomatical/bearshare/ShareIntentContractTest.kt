package com.inomatical.bearshare

import org.junit.Assert.assertFalse
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ShareIntentContractTest {
    @Test
    fun acceptsSingleItemShareWithAnySupportedMimeType() {
        assertTrue(ShareIntentContract.accepts(ShareIntentContract.SendAction, "text/plain"))
        assertTrue(ShareIntentContract.accepts(ShareIntentContract.SendAction, "image/jpeg"))
        assertTrue(ShareIntentContract.accepts(ShareIntentContract.SendAction, "application/pdf"))
    }

    @Test
    fun acceptsMultipleItemShares() {
        assertTrue(ShareIntentContract.accepts(ShareIntentContract.SendMultipleAction, "image/*"))
    }

    @Test
    fun rejectsNonShareActionsAndMissingMimeTypes() {
        assertFalse(ShareIntentContract.accepts("android.intent.action.VIEW", "text/plain"))
        assertFalse(ShareIntentContract.accepts(ShareIntentContract.SendAction, null))
    }

    @Test
    fun extractsLiteralPlainTextFromSingleItemShare() {
        assertEquals(
            "Hello, \u4e16\u754c",
            ShareIntentContract.plainText(ShareIntentContract.SendAction, "text/plain", "Hello, \u4e16\u754c"),
        )
    }

    @Test
    fun rejectsNonTextAndIncompletePlainTextShares() {
        assertNull(ShareIntentContract.plainText(ShareIntentContract.SendMultipleAction, "text/plain", "Hello"))
        assertNull(ShareIntentContract.plainText(ShareIntentContract.SendAction, "image/png", "Hello"))
        assertNull(ShareIntentContract.plainText(ShareIntentContract.SendAction, "text/plain", null))
    }

    @Test
    fun formatsConfiguredTargetAsEagleShareItemsEndpoint() {
        assertEquals(
            "http://192.168.1.20:61432/api/items",
            TextShareDelivery.endpointFor(EagleShareTarget("192.168.1.20", 61432)).toString(),
        )
        assertEquals(
            "http://[2001:db8::1]:61432/api/items",
            TextShareDelivery.endpointFor(EagleShareTarget("2001:db8::1", 61432)).toString(),
        )
    }
}