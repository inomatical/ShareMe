package com.inomatical.bearshare

import android.app.Activity
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.provider.OpenableColumns
import android.text.InputType
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast

class MainActivity : Activity() {
    private val failedBatchFiles = FailedBatchFileState()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        if (!handleSharedText(intent) && !handleSharedImage(intent) && !handleSharedFile(intent) && !handleSharedFiles(intent)) {
            showFailedOutgoingOrTargetConfiguration()
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)

        if (!handleSharedText(intent) && !handleSharedImage(intent) && !handleSharedFile(intent) && !handleSharedFiles(intent)) {
            showFailedOutgoingOrTargetConfiguration()
        }
    }

    private fun showTargetConfiguration() {

        val targetPreferences = TargetPreferences(this)
        val currentTarget = targetPreferences.load()
        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val addressInput = EditText(this).apply {
            hint = getString(R.string.target_address_hint)
            inputType = InputType.TYPE_CLASS_TEXT
            setText(currentTarget?.address.orEmpty())
        }
        val portInput = EditText(this).apply {
            hint = getString(R.string.target_port_hint)
            inputType = InputType.TYPE_CLASS_NUMBER
            setText((currentTarget?.port ?: DEFAULT_PORT).toString())
        }

        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)

                addView(TextView(this@MainActivity).apply {
                    setText(R.string.target_configuration_title)
                    textSize = 22f
                })
                addView(TextView(this@MainActivity).apply {
                    setText(R.string.target_configuration_description)
                })
                addView(addressInput, fullWidthLayoutParams())
                addView(portInput, fullWidthLayoutParams())
                addView(Button(this@MainActivity).apply {
                    setText(R.string.save_target)
                    setOnClickListener {
                        when (val validation = TargetConfiguration.validate(addressInput.text.toString(), portInput.text.toString())) {
                            is TargetValidationResult.Valid -> {
                                addressInput.error = null
                                portInput.error = null
                                targetPreferences.save(validation.target)
                                Toast.makeText(this@MainActivity, R.string.target_saved, Toast.LENGTH_SHORT).show()
                            }
                            is TargetValidationResult.Invalid -> {
                                addressInput.error = validation.addressError
                                portInput.error = validation.portError
                            }
                        }
                    }
                }, fullWidthLayoutParams())
            },
        )
    }

    private fun handleSharedText(intent: Intent): Boolean {
        val text = ShareIntentContract.plainText(
            intent.action,
            intent.type,
            intent.getCharSequenceExtra(Intent.EXTRA_TEXT),
        ) ?: return false
        val itemKind = ShareIntentContract.itemKind(text)
        sendOutgoingText(FailedOutgoingItem(itemKind, text))
        return true
    }

    private fun showFailedOutgoingOrTargetConfiguration() {
        val item = FailedOutgoingPreferences(this).load()
        if (item == null) {
            showTargetConfiguration()
        } else {
            showFailedOutgoing(item, "Previous delivery failed.")
        }
    }

    private fun sendOutgoingText(item: FailedOutgoingItem) {
        val itemLabel = if (item.kind == "url") "URL" else "Text"
        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val statusText = TextView(this).apply {
            textSize = 18f
            setText("Sending $itemLabel to EagleShare...")
        }
        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)
                addView(TextView(this@MainActivity).apply {
                    textSize = 22f
                    setText("BearShare")
                })
                addView(statusText, fullWidthLayoutParams())
            },
        )

        val target = TargetPreferences(this).load()
        if (target == null) {
            FailedOutgoingPreferences(this).save(item)
            showFailedOutgoing(item, "Delivery failed: configure an EagleShare target first.")
            return
        }

        Thread {
            val result = TextShareDelivery.send(target, item.kind, item.content)
            runOnUiThread {
                when (result) {
                    TextDeliveryResult.Success -> {
                        FailedOutgoingPreferences(this).discard()
                        statusText.text = "$itemLabel delivered to EagleShare."
                    }
                    is TextDeliveryResult.Failure -> {
                        FailedOutgoingPreferences(this).save(item)
                        showFailedOutgoing(item, "Delivery failed: ${result.message}")
                    }
                }
            }
        }.start()
    }

    private fun showFailedOutgoing(item: FailedOutgoingItem, message: String) {
        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val statusText = TextView(this).apply {
            textSize = 18f
            text = message
        }
        val retryButton = Button(this).apply {
            text = "Retry"
            setOnClickListener { sendOutgoingText(item) }
        }
        val discardButton = Button(this).apply {
            text = "Discard"
            setOnClickListener {
                FailedOutgoingPreferences(this@MainActivity).discard()
                statusText.text = "Failed outgoing item discarded."
                isEnabled = false
                retryButton.isEnabled = false
            }
        }
        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)
                addView(TextView(this@MainActivity).apply {
                    textSize = 22f
                    text = "BearShare"
                })
                addView(statusText, fullWidthLayoutParams())
                addView(retryButton, fullWidthLayoutParams())
                addView(discardButton, fullWidthLayoutParams())
            },
        )
    }

    @Suppress("DEPRECATION")
    private fun handleSharedImage(intent: Intent): Boolean {
        val imageUri = intent.getParcelableExtra<Uri>(Intent.EXTRA_STREAM)
        if (!ShareIntentContract.acceptsSingleImage(intent.action, intent.type, imageUri != null)) {
            return false
        }

        val mimeType = intent.type!!
        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val statusText = TextView(this).apply {
            textSize = 18f
            setText("Sending image to EagleShare...")
        }
        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)
                addView(TextView(this@MainActivity).apply {
                    textSize = 22f
                    setText("BearShare")
                })
                addView(statusText, fullWidthLayoutParams())
            },
        )

        val target = TargetPreferences(this).load()
        if (target == null) {
            statusText.setText("Delivery failed: configure an EagleShare target first.")
            return true
        }

        val fileName = sharedFileName(imageUri!!)
        Thread {
            val result = try {
                contentResolver.openInputStream(imageUri)?.use { content ->
                    ImageShareDelivery.send(target, mimeType, fileName, content)
                } ?: ImageDeliveryResult.Failure("Could not open the shared image.")
            } catch (_: SecurityException) {
                ImageDeliveryResult.Failure("Could not open the shared image.")
            }
            val status = when (result) {
                ImageDeliveryResult.Success -> "Image delivered to EagleShare."
                is ImageDeliveryResult.Failure -> "Delivery failed: ${result.message}"
            }
            runOnUiThread { statusText.text = status }
        }.start()
        return true
    }

    @Suppress("DEPRECATION")
    private fun handleSharedFile(intent: Intent): Boolean {
        val fileUri = intent.getParcelableExtra<Uri>(Intent.EXTRA_STREAM)
        if (!ShareIntentContract.acceptsSingleFile(intent.action, intent.type, fileUri != null)) {
            return false
        }

        val mimeType = intent.type!!
        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val statusText = TextView(this).apply {
            textSize = 18f
            setText("Sending file to EagleShare...")
        }
        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)
                addView(TextView(this@MainActivity).apply {
                    textSize = 22f
                    setText("BearShare")
                })
                addView(statusText, fullWidthLayoutParams())
            },
        )

        val target = TargetPreferences(this).load()
        if (target == null) {
            statusText.setText("Delivery failed: configure an EagleShare target first.")
            return true
        }

        val fileName = sharedFileName(fileUri!!)
        Thread {
            val result = try {
                contentResolver.openInputStream(fileUri)?.use { content ->
                    FileShareDelivery.send(target, mimeType, fileName, content)
                } ?: FileDeliveryResult.Failure("Could not open the shared file.")
            } catch (_: SecurityException) {
                FileDeliveryResult.Failure("Could not open the shared file.")
            }
            val status = when (result) {
                FileDeliveryResult.Success -> "File delivered to EagleShare."
                is FileDeliveryResult.Failure -> "Delivery failed: ${result.message}"
            }
            runOnUiThread { statusText.text = status }
        }.start()
        return true
    }

    @Suppress("DEPRECATION")
    private fun handleSharedFiles(intent: Intent): Boolean {
        val fileUris = intent.getParcelableArrayListExtra<Uri>(Intent.EXTRA_STREAM).orEmpty()
        if (!ShareIntentContract.acceptsMultipleFiles(intent.action, intent.type, fileUris.size)) {
            return false
        }

        val contentPadding = (24 * resources.displayMetrics.density).toInt()
        val statusText = TextView(this).apply {
            textSize = 18f
            setText("Sending ${fileUris.size} files to EagleShare...")
        }
        val results = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
        }
        setContentView(
            LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(contentPadding, contentPadding, contentPadding, contentPadding)
                addView(TextView(this@MainActivity).apply {
                    textSize = 22f
                    setText("BearShare")
                })
                addView(statusText, fullWidthLayoutParams())
                addView(results, fullWidthLayoutParams())
            },
        )

        val target = TargetPreferences(this).load()
        if (target == null) {
            statusText.setText("Delivery failed: configure an EagleShare target first.")
            fileUris.forEach { uri -> addBatchFileResult(results, uri, FileDeliveryResult.Failure("Not sent.")) }
            return true
        }

        Thread {
            SequentialFileDelivery.send(
                fileUris,
                sendItem = { uri -> sendSharedFile(target, intent.type, uri) },
                onOutcome = { outcome ->
                    runOnUiThread { addBatchFileResult(results, outcome.item, outcome.result) }
                },
            )
            runOnUiThread { statusText.text = "File delivery complete." }
        }.start()
        return true
    }

    private fun sendSharedFile(target: EagleShareTarget, shareMimeType: String?, uri: Uri): FileDeliveryResult =
        try {
            val mimeType = contentResolver.getType(uri) ?: shareMimeType ?: "application/octet-stream"
            contentResolver.openInputStream(uri)?.use { content ->
                FileShareDelivery.send(target, mimeType, sharedFileName(uri), content)
            } ?: FileDeliveryResult.Failure("Could not open the shared file.")
        } catch (_: SecurityException) {
            FileDeliveryResult.Failure("Could not open the shared file.")
        }

    private fun addBatchFileResult(results: LinearLayout, uri: Uri, result: FileDeliveryResult) {
        val fileName = sharedFileName(uri)
        val statusText = TextView(this).apply {
            text = "$fileName: ${fileResultText(result)}"
        }
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            addView(statusText, LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f))
        }

        if (result is FileDeliveryResult.Failure) {
            val failedItem = FailedBatchFile(uri.toString(), fileName)
            failedBatchFiles.record(failedItem, result)
            val retryButton = Button(this).apply { text = "Retry" }
            val discardButton = Button(this).apply { text = "Discard" }
            retryButton.setOnClickListener {
                retryButton.isEnabled = false
                discardButton.isEnabled = false
                statusText.text = "$fileName: Retrying..."
                retryBatchFile(failedItem, uri, statusText, retryButton, discardButton)
            }
            discardButton.setOnClickListener {
                failedBatchFiles.discard(failedItem.uri)
                statusText.text = "$fileName: Discarded"
                retryButton.isEnabled = false
                discardButton.isEnabled = false
            }
            row.addView(retryButton)
            row.addView(discardButton)
        }

        results.addView(row, fullWidthLayoutParams())
    }

    private fun retryBatchFile(
        failedItem: FailedBatchFile,
        uri: Uri,
        statusText: TextView,
        retryButton: Button,
        discardButton: Button,
    ) {
        Thread {
            val target = TargetPreferences(this).load()
            val result = if (target == null) {
                FileDeliveryResult.Failure("Configure an EagleShare target first.")
            } else {
                sendSharedFile(target, null, uri)
            }
            runOnUiThread {
                failedBatchFiles.record(failedItem, result)
                statusText.text = "${failedItem.fileName}: ${fileResultText(result)}"
                if (result is FileDeliveryResult.Success) {
                    retryButton.visibility = View.GONE
                    discardButton.visibility = View.GONE
                } else {
                    retryButton.isEnabled = true
                    discardButton.isEnabled = true
                }
            }
        }.start()
    }

    private fun fileResultText(result: FileDeliveryResult): String = when (result) {
        FileDeliveryResult.Success -> "Delivered"
        is FileDeliveryResult.Failure -> "Failed: ${result.message}"
    }

    private fun sharedFileName(uri: Uri): String =
        contentResolver.query(uri, arrayOf(OpenableColumns.DISPLAY_NAME), null, null, null)?.use { cursor ->
            if (cursor.moveToFirst()) cursor.getString(0) else null
        } ?: uri.lastPathSegment ?: "shared-file"

    private fun fullWidthLayoutParams() = LinearLayout.LayoutParams(
        ViewGroup.LayoutParams.MATCH_PARENT,
        ViewGroup.LayoutParams.WRAP_CONTENT,
    )

    private companion object {
        const val DEFAULT_PORT = 61432
    }
}