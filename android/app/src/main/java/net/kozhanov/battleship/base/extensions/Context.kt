package net.kozhanov.battleship.base.extensions

import android.app.AlertDialog
import android.content.Context
import android.content.pm.PackageManager
import android.content.pm.ShortcutManager
import android.graphics.drawable.Drawable
import android.os.Build
import android.util.TypedValue
import android.view.View
import android.view.inputmethod.InputMethodManager
import androidx.annotation.AttrRes
import androidx.annotation.ColorInt
import androidx.annotation.ColorRes
import androidx.annotation.DrawableRes
import androidx.annotation.RequiresApi
import androidx.core.content.ContextCompat
import timber.log.Timber

val Context.inputMethodManager: InputMethodManager
    get() = checkNotNull(ContextCompat.getSystemService(this, InputMethodManager::class.java))

val Context.shortcutManager: ShortcutManager
    @RequiresApi(Build.VERSION_CODES.N_MR1)
    get() = checkNotNull(ContextCompat.getSystemService(this, ShortcutManager::class.java))

@Suppress("unused")
@ColorInt
fun Context.color(@ColorRes colorRes: Int): Int {
    return ContextCompat.getColor(this, colorRes)
}

fun Context.drawable(@DrawableRes drawableRes: Int): Drawable {
    return checkNotNull(ContextCompat.getDrawable(this, drawableRes))
}

fun Context.dpToPx(dp: Float): Int = (dp * resources.displayMetrics.density).toInt()

val Context.displayHeightPx get() = resources.displayMetrics.heightPixels

@ColorInt
fun Context.resolveThemeColor(@AttrRes attrResColor: Int): Int {
    val typedValue = TypedValue()
    theme.resolveAttribute(attrResColor, typedValue, true)
    return typedValue.data
}

fun Context.drawableWithColor(@DrawableRes drawableRes: Int, @AttrRes attrResColor: Int): Drawable {
    return checkNotNull(ContextCompat.getDrawable(this, drawableRes))
        .apply {
            setTint(resolveThemeColor(attrResColor))
        }
}

data class AlertDialogButton(
    val text: Int,
    val onClicked: () -> Unit
)

@Suppress("LongParameterList")
fun Context.showAlertDialog(
    title: String? = null,
    message: String? = null,
    positiveButton: AlertDialogButton? = null,
    negativeButton: AlertDialogButton? = null,
    neutralButton: AlertDialogButton? = null,
    customView: View? = null,
    @DrawableRes icon: Int? = null,
    cancelable: Boolean = true
): AlertDialog {
    val builder = AlertDialog.Builder(this)
    title?.let {
        builder.setTitle(it)
    }
    message?.let {
        builder.setMessage(it)
    }
    positiveButton?.let { button ->
        builder.setPositiveButton(button.text) { _, _ ->
            button.onClicked()
        }
    }
    negativeButton?.let { button ->
        builder.setNegativeButton(button.text) { _, _ ->
            button.onClicked()
        }
    }
    neutralButton?.let { button ->
        builder.setNeutralButton(button.text) { _, _ ->
            button.onClicked()
        }
    }
    customView?.let { view ->
        builder.setView(view)
    }
    icon?.let { drawableIcon ->
        builder.setIcon(drawableIcon)
    }
    builder.setCancelable(cancelable)
    val dialog = builder.create()
    builder.show()
    return dialog
}
