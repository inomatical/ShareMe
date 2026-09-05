package com.inomatical.bearshare

import android.app.Activity
import android.os.Bundle
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

    private fun fullWidthLayoutParams() = LinearLayout.LayoutParams(
        ViewGroup.LayoutParams.MATCH_PARENT,
        ViewGroup.LayoutParams.WRAP_CONTENT,
    )

    private companion object {
        const val DEFAULT_PORT = 61432
    }
}