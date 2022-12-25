package net.kozhanov.battleship.base.extensions

import android.view.MenuItem
import android.view.View
import com.google.android.material.bottomnavigation.BottomNavigationView

private const val DEFAULT_THROTTLE_DELAY = 200L

fun View.setThrottledClickListener(delay: Long = 200L, onClicked: (View) -> Unit) {
    setOnClickListener {
        throttle(delay) {
            onClicked(it)
        }
    }
}

fun BottomNavigationView.setThrottledNavigationItemSelectedListener(
    delay: Long = DEFAULT_THROTTLE_DELAY,
    onSelect: (MenuItem) -> Unit
) {
    setOnItemSelectedListener {
        throttle(delay) {
            onSelect(it)
        }
    }
}

private var lastClickTimestamp = 0L
private fun throttle(delay: Long = DEFAULT_THROTTLE_DELAY, action: () -> Unit): Boolean {
    val currentTimestamp = System.currentTimeMillis()
    val delta = currentTimestamp - lastClickTimestamp
    if (delta !in 0L..delay) {
        lastClickTimestamp = currentTimestamp
        action()
        return true
    }
    return false
}
