package com.inomatical.bearshare

internal data class FailedOutgoingItem(val kind: String, val content: String)

internal class FailedOutgoingState {
    var item: FailedOutgoingItem? = null
        private set

    fun retain(failedItem: FailedOutgoingItem) {
        item = failedItem
    }

    fun completeAttempt(attemptedItem: FailedOutgoingItem, result: TextDeliveryResult) {
        if (result is TextDeliveryResult.Success && item == attemptedItem) {
            item = null
        } else if (result is TextDeliveryResult.Failure) {
            item = attemptedItem
        }
    }

    fun discard() {
        item = null
    }
}