package net.kozhanov.battleship.base.extensions

import android.app.Activity

fun Activity.hideKeyboard(flags: Int = 0) {
    inputMethodManager.hideSoftInputFromWindow(window.decorView.windowToken, flags)
}
