package com.inomatical.bearshare

import java.net.URI

internal object ShareIntentContract {
    const val SendAction = "android.intent.action.SEND"
    const val SendMultipleAction = "android.intent.action.SEND_MULTIPLE"

    fun accepts(action: String?, mimeType: String?): Boolean =
        action in setOf(SendAction, SendMultipleAction) && !mimeType.isNullOrBlank()

    fun plainText(action: String?, mimeType: String?, content: CharSequence?): String? =
        if (action == SendAction && mimeType == "text/plain" && content != null) {
            content.toString()
        } else {
            null
        }

    fun acceptsSingleImage(action: String?, mimeType: String?, hasContentStream: Boolean): Boolean =
        action == SendAction && mimeType?.startsWith("image/") == true && hasContentStream

    fun itemKind(content: String): String =
        if (runCatching { URI(content).isAbsolute }.getOrDefault(false)) "url" else "text"
}