package com.inomatical.bearshare

import java.io.IOException
import java.net.HttpURLConnection
import java.net.URL
import java.util.UUID

sealed interface TextDeliveryResult {
    data object Success : TextDeliveryResult

    data class Failure(val message: String) : TextDeliveryResult
}

internal object TextShareDelivery {
    fun send(target: EagleShareTarget, text: String): TextDeliveryResult {
        return try {
            val boundary = "BearShare-${UUID.randomUUID()}"
            val connection = (endpointFor(target).openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = CONNECTION_TIMEOUT_MILLIS
                readTimeout = READ_TIMEOUT_MILLIS
                doOutput = true
                setRequestProperty("Content-Type", "multipart/form-data; boundary=$boundary")
            }

            connection.outputStream.use { output ->
                output.write(multipartBody(boundary, text))
            }

            val result = if (connection.responseCode in 200..299) {
                TextDeliveryResult.Success
            } else {
                TextDeliveryResult.Failure("EagleShare returned HTTP ${connection.responseCode}.")
            }
            connection.disconnect()
            result
        } catch (_: IOException) {
            TextDeliveryResult.Failure("Could not reach EagleShare.")
        }
    }

    internal fun endpointFor(target: EagleShareTarget): URL {
        val address = if (target.address.contains(':')) "[${target.address}]" else target.address
        return URL("http://$address:${target.port}/api/items")
    }

    private fun multipartBody(boundary: String, text: String): ByteArray = buildString {
        append("--$boundary\r\n")
        append("Content-Disposition: form-data; name=\"kind\"\r\n\r\n")
        append("text\r\n")
        append("--$boundary\r\n")
        append("Content-Disposition: form-data; name=\"content\"\r\n")
        append("Content-Type: text/plain; charset=UTF-8\r\n\r\n")
        append(text)
        append("\r\n--$boundary--\r\n")
    }.toByteArray(Charsets.UTF_8)

    private const val CONNECTION_TIMEOUT_MILLIS = 5_000
    private const val READ_TIMEOUT_MILLIS = 10_000
}