package com.inomatical.bearshare

internal data class FileDeliveryOutcome<T>(val item: T, val result: FileDeliveryResult)

internal object SequentialFileDelivery {
    fun <T> send(
        items: Iterable<T>,
        sendItem: (T) -> FileDeliveryResult,
        onOutcome: (FileDeliveryOutcome<T>) -> Unit = {},
    ): List<FileDeliveryOutcome<T>> {
        var shouldSendNext = true
        return items.map { item ->
            val result = if (shouldSendNext) {
                sendItem(item).also { shouldSendNext = it is FileDeliveryResult.Success }
            } else {
                FileDeliveryResult.Failure("Not sent because a previous file failed.")
            }
            FileDeliveryOutcome(item, result).also(onOutcome)
        }
    }
}