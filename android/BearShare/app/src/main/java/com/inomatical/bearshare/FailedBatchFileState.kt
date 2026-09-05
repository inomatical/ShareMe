package com.inomatical.bearshare

internal data class FailedBatchFile(val uri: String, val fileName: String)

internal class FailedBatchFileState {
    private val failedItems = linkedMapOf<String, FailedBatchFile>()

    fun record(item: FailedBatchFile, result: FileDeliveryResult) {
        if (result is FileDeliveryResult.Failure) {
            failedItems[item.uri] = item
        } else {
            failedItems.remove(item.uri)
        }
    }

    fun discard(uri: String) {
        failedItems.remove(uri)
    }

    fun contains(uri: String): Boolean = uri in failedItems

    fun items(): List<FailedBatchFile> = failedItems.values.toList()
}