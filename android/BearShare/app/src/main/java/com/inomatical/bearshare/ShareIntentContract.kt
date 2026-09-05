package com.inomatical.bearshare

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
}