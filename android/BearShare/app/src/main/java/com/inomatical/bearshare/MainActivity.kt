package com.inomatical.bearshare

import android.app.Activity
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.provider.OpenableColumns
import android.text.InputType
import android.view.ViewGroup
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast

class MainActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        if (!handleSharedText(intent) && !handleSharedImage(intent) && !handleSharedFile(intent)) {
            showTargetConfiguration()
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)

        if (!handleSharedText(intent) && !handleSharedImage(intent) && !handleSharedFile(intent)) {
            showTargetConfiguration()
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
        val itemLabel = if (itemKind == "url") "URL" else "Text"

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
            statusText.setText("Delivery failed: configure an EagleShare target first.")
            return true
        }

        Thread {
            val status = when (val result = TextShareDelivery.send(target, itemKind, text)) {
                TextDeliveryResult.Success -> "$itemLabel delivered to EagleShare."
                is TextDeliveryResult.Failure -> "Delivery failed: ${result.message}"
            }
            runOnUiThread { statusText.text = status }
        }.start()
        return true
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