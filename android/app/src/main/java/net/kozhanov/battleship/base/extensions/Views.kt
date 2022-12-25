@file:Suppress("TooManyFunctions")

package net.kozhanov.battleship.base.extensions

import android.graphics.drawable.Drawable
import android.view.View
import android.view.ViewTreeObserver
import android.view.inputmethod.InputMethodManager
import androidx.annotation.AttrRes
import androidx.annotation.ColorInt
import androidx.annotation.ColorRes
import androidx.annotation.DrawableRes
import androidx.core.content.ContextCompat
import com.google.android.material.snackbar.Snackbar

@Suppress("unused")
@ColorInt
fun View.color(@ColorRes colorRes: Int): Int {
    return ContextCompat.getColor(context, colorRes)
}

fun View.drawable(@DrawableRes drawableRes: Int): Drawable {
    return checkNotNull(ContextCompat.getDrawable(context, drawableRes))
}

@ColorInt
fun View.resolveThemeColor(@AttrRes attrResColor: Int): Int {
    return context.resolveThemeColor(attrResColor)
}

fun View.showSnackMessage(message: String) {
    Snackbar
        .make(this, message, Snackbar.LENGTH_LONG)
        .show()
}

fun View.showColoredSnackMessage(
    message: String,
    @AttrRes backgroundColor: Int,
    @AttrRes textColor: Int
) {
    Snackbar
        .make(this, message, Snackbar.LENGTH_LONG)
        .setBackgroundTint(resolveThemeColor(backgroundColor))
        .setTextColor(resolveThemeColor(textColor))
        .show()
}

const val DELAY_FOR_REQUEST_FIELD_ACTION = 200L
inline fun View.postWithDelay(
    delayInMillis: Long = DELAY_FOR_REQUEST_FIELD_ACTION,
    crossinline action: () -> Unit
): Runnable {
    val runnable = Runnable { action() }
    postDelayed(runnable, delayInMillis)
    return runnable
}

fun View.focusAndShowKeyboardWithDelay(delay: Long = DELAY_FOR_REQUEST_FIELD_ACTION) {
    postWithDelay(delay) { focusAndShowKeyboard() }
}

fun View.focusAndShowKeyboard() {
    fun View.showTheKeyboardNow() {
        if (isFocused) {
            post {
                context.inputMethodManager.showSoftInput(this, InputMethodManager.SHOW_IMPLICIT)
            }
        }
    }
    requestFocus()
    if (hasWindowFocus()) {
        showTheKeyboardNow()
    } else {
        viewTreeObserver.addOnWindowFocusChangeListener(
            object : ViewTreeObserver.OnWindowFocusChangeListener {
                override fun onWindowFocusChanged(hasFocus: Boolean) {
                    if (hasFocus) {
                        this@focusAndShowKeyboard.showTheKeyboardNow()
                        viewTreeObserver.removeOnWindowFocusChangeListener(this)
                    }
                }
            })
    }
}

fun View.showIf(block: () -> Boolean) {
    visibility = if (block()) {
        View.VISIBLE
    } else {
        View.GONE
    }
}

fun View.enableIf(block: () -> Boolean) {
    isEnabled = block()
}

private const val ALPHA_IF_NOT = 1f
private const val ALPHA_IF = 0.2f
fun View.alphaIf(alphaIfNot: Float = ALPHA_IF_NOT, alphaIf: Float = ALPHA_IF, block: () -> Boolean) {
    alpha = if (block()) {
        alphaIf
    } else
        alphaIfNot
}
