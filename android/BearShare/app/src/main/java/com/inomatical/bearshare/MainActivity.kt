package com.inomatical.bearshare

import android.app.Activity
import android.os.Bundle
import android.view.Gravity
import android.widget.TextView

class MainActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(
            TextView(this).apply {
                gravity = Gravity.CENTER
                setText(R.string.app_ready)
                textSize = 18f
            },
        )
    }
}