package com.inomatical.bearshare

import org.junit.Assert.assertFalse
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream

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
    fun acceptsSingleImageSharesOnlyWhenTheImageStreamIsPresent() {
        assertTrue(ShareIntentContract.acceptsSingleImage(ShareIntentContract.SendAction, "image/png", true))
        assertTrue(ShareIntentContract.acceptsSingleImage(ShareIntentContract.SendAction, "image/jpeg", true))
        assertFalse(ShareIntentContract.acceptsSingleImage(ShareIntentContract.SendAction, "image/png", false))
        assertFalse(ShareIntentContract.acceptsSingleImage(ShareIntentContract.SendMultipleAction, "image/png", true))
        assertFalse(ShareIntentContract.acceptsSingleImage(ShareIntentContract.SendAction, "text/plain", true))
    }

    @Test
    fun classifiesAbsoluteUrlSharesSeparatelyFromPlainText() {
        assertEquals("url", ShareIntentContract.itemKind("https://example.com/path?query=value#section"))
        assertEquals("text", ShareIntentContract.itemKind("A literal note with a URL: https://example.com"))
        assertEquals("text", ShareIntentContract.itemKind("example.com"))
    }

    @Test
    fun encodesUrlKindAndLiteralValueInMultipartDelivery() {
        val multipart = TextShareDelivery.multipartBody("test-boundary", "url", "https://example.com/a?b=c")
            .toString(Charsets.UTF_8)

        assertTrue(multipart.contains("name=\"kind\"\r\n\r\nurl\r\n"))
        assertTrue(multipart.contains("https://example.com/a?b=c"))
    }

    @Test
    fun streamsImageBytesInMultipartDelivery() {
        val imageBytes = byteArrayOf(1, 2, 3, 4)
        val output = ByteArrayOutputStream()

        ImageShareDelivery.writeMultipart(
            output,
            "test-boundary",
            "image/png",
            "holiday.png",
            ByteArrayInputStream(imageBytes),
        )

        val multipart = output.toByteArray()
        assertTrue(multipart.toString(Charsets.UTF_8).contains("name=\"kind\"\r\n\r\nimage\r\n"))
        assertTrue(multipart.toString(Charsets.UTF_8).contains("filename=\"holiday.png\""))
        assertTrue(multipart.asList().windowed(imageBytes.size).any { it.toByteArray().contentEquals(imageBytes) })
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