package ru.openbank.accept.base.extensions

import androidx.fragment.app.Fragment
import net.kozhanov.battleship.base.extensions.hideKeyboard

fun Fragment.hideKeyboard() {
    requireActivity().hideKeyboard()
}
