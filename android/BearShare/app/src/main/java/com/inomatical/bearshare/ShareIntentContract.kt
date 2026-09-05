package com.inomatical.bearshare

internal object ShareIntentContract {
    const val SendAction = "android.intent.action.SEND"
    const val SendMultipleAction = "android.intent.action.SEND_MULTIPLE"

    fun accepts(action: String?, mimeType: String?): Boolean =
        action in setOf(SendAction, SendMultipleAction) && !mimeType.isNullOrBlank()
}