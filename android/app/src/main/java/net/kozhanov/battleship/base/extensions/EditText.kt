package ru.openbank.accept.base.extensions

import android.view.inputmethod.EditorInfo
import android.widget.EditText

fun EditText.setOnDoneActionListener(action: () -> Unit) {
    this.setOnActionListener(EditorInfo.IME_ACTION_DONE, action)
}

private fun EditText.setOnActionListener(actionId: Int, action: () -> Unit) {
    this.setOnEditorActionListener { _, currentActionId, _ ->
        if (currentActionId == actionId) {
            action()
            true
        } else {
            false
        }
    }
}

fun EditText.updateText(newText: String) {
    if (text.toString() != newText) {
        setText(newText)
    }
}
