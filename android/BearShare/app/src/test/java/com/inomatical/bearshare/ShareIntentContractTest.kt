package com.inomatical.bearshare

import org.junit.Assert.assertFalse
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
}