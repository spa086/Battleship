package net.kozhanov.battleship.base.core.platform

import android.os.Parcelable
import androidx.core.os.bundleOf
import androidx.fragment.app.Fragment

private const val KEY = "ARGUMENTS_KEY"

/**
 * Интерфейс для удобной передачи аргументов во фрагмент.
 */
interface ParcelableScreenArguments : Parcelable

@Suppress("UNCHECKED_CAST")
fun <T : ParcelableScreenArguments> Fragment.getScreenArguments() = requireArguments()
    .getParcelable<ParcelableScreenArguments>(KEY, ParcelableScreenArguments::class.java) as T

fun <T : ParcelableScreenArguments> Fragment.saveScreenArguments(screenArguments: T) {
    arguments = bundleOf(KEY to screenArguments)
}
