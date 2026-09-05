package com.inomatical.bearshare

import java.io.IOException
import java.io.InputStream
import java.io.OutputStream
import java.net.HttpURLConnection
import java.util.UUID

sealed interface FileDeliveryResult {
    data object Success : FileDeliveryResult

    data class Failure(val message: String) : FileDeliveryResult
}

internal object FileShareDelivery {
    fun send(
        target: EagleShareTarget,
        mimeType: String,
        fileName: String,
        content: InputStream,
    ): FileDeliveryResult {
        var connection: HttpURLConnection? = null
        return try {
            val boundary = "BearShare-${UUID.randomUUID()}"
            connection = (TextShareDelivery.endpointFor(target).openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                connectTimeout = CONNECTION_TIMEOUT_MILLIS
                readTimeout = READ_TIMEOUT_MILLIS
                doOutput = true
                setRequestProperty("Content-Type", "multipart/form-data; boundary=$boundary")
            }

            connection.outputStream.buffered().use { output ->
                writeMultipart(output, boundary, mimeType, fileName, content)
            }

            if (connection.responseCode in 200..299) {
                FileDeliveryResult.Success
            } else {
                FileDeliveryResult.Failure("EagleShare returned HTTP ${connection.responseCode}.")
            }
        } catch (_: IOException) {
            FileDeliveryResult.Failure("Could not reach EagleShare.")
        } finally {
            connection?.disconnect()
        }
    }

    internal fun writeMultipart(
        output: OutputStream,
        boundary: String,
        mimeType: String,
        fileName: String,
        content: InputStream,
    ) {
        output.write("--$boundary\r\n".toByteArray())
        output.write("Content-Disposition: form-data; name=\"kind\"\r\n\r\nfile\r\n".toByteArray())
        output.write("--$boundary\r\n".toByteArray())
        output.write(
            "Content-Disposition: form-data; name=\"content\"; filename=\"${fileName.replace("\"", "_")}\"\r\n".toByteArray(),
        )
        output.write("Content-Type: $mimeType\r\n\r\n".toByteArray())
        content.copyTo(output, bufferSize = BUFFER_SIZE)
        output.write("\r\n--$boundary--\r\n".toByteArray())
    }

    private const val CONNECTION_TIMEOUT_MILLIS = 5_000
    private const val READ_TIMEOUT_MILLIS = 10_000
    private const val BUFFER_SIZE = 81_920
}