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
    fun acceptsSingleFileSharesOnlyForNonTextAndNonImageContent() {
        assertTrue(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendAction, "application/pdf", true))
        assertTrue(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendAction, "application/zip", true))
        assertFalse(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendAction, "application/pdf", false))
        assertFalse(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendAction, "text/plain", true))
        assertFalse(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendAction, "image/png", true))
        assertFalse(ShareIntentContract.acceptsSingleFile(ShareIntentContract.SendMultipleAction, "application/pdf", true))
    }

    @Test
    fun acceptsMultipleFileSharesOnlyWhenFilesArePresent() {
        assertTrue(ShareIntentContract.acceptsMultipleFiles(ShareIntentContract.SendMultipleAction, "*/*", 2))
        assertFalse(ShareIntentContract.acceptsMultipleFiles(ShareIntentContract.SendMultipleAction, "*/*", 0))
        assertFalse(ShareIntentContract.acceptsMultipleFiles(ShareIntentContract.SendAction, "*/*", 2))
    }

    @Test
    fun sendsFilesSequentiallyAndMarksItemsAfterAFailure() {
        val items = listOf("first", "second", "third")
        val attemptedItems = mutableListOf<String>()

        val outcomes = SequentialFileDelivery.send(items, sendItem = { item ->
            attemptedItems += item
            if (item == "second") FileDeliveryResult.Failure("EagleShare rejected the file.")
            else FileDeliveryResult.Success
        })

        assertEquals(listOf("first", "second"), attemptedItems)
        assertTrue(outcomes[0].result is FileDeliveryResult.Success)
        assertTrue(outcomes[1].result is FileDeliveryResult.Failure)
        assertEquals("Not sent because a previous file failed.", (outcomes[2].result as FileDeliveryResult.Failure).message)
    }

    @Test
    fun retainsFailedOutgoingTextUntilRetrySucceedsOrUserDiscardsIt() {
        val state = FailedOutgoingState()
        val item = FailedOutgoingItem("text", "A retryable note")

        state.completeAttempt(item, TextDeliveryResult.Failure("Could not reach EagleShare."))
        assertEquals(item, state.item)

        state.completeAttempt(item, TextDeliveryResult.Success)
        assertNull(state.item)

        state.retain(item)
        state.discard()
        assertNull(state.item)
    }

    @Test
    fun retainsOnlyFailedBatchFilesForIndependentRetryOrDiscard() {
        val state = FailedBatchFileState()
        val delivered = FailedBatchFile("content://share/delivered", "delivered.pdf")
        val failed = FailedBatchFile("content://share/failed", "failed.pdf")

        state.record(delivered, FileDeliveryResult.Success)
        state.record(failed, FileDeliveryResult.Failure("Could not reach EagleShare."))

        assertFalse(state.contains(delivered.uri))
        assertTrue(state.contains(failed.uri))

        state.record(failed, FileDeliveryResult.Success)
        assertFalse(state.contains(failed.uri))

        state.record(failed, FileDeliveryResult.Failure("EagleShare rejected the file."))
        state.discard(failed.uri)
        assertTrue(state.items().isEmpty())
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
    fun streamsFileBytesInMultipartDelivery() {
        val fileBytes = byteArrayOf(5, 6, 7, 8)
        val output = ByteArrayOutputStream()

        FileShareDelivery.writeMultipart(
            output,
            "test-boundary",
            "application/pdf",
            "report.pdf",
            ByteArrayInputStream(fileBytes),
        )

        val multipart = output.toByteArray()
        assertTrue(multipart.toString(Charsets.UTF_8).contains("name=\"kind\"\r\n\r\nfile\r\n"))
        assertTrue(multipart.toString(Charsets.UTF_8).contains("filename=\"report.pdf\""))
        assertTrue(multipart.asList().windowed(fileBytes.size).any { it.toByteArray().contentEquals(fileBytes) })
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