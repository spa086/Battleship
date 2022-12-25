package net.kozhanov.battleship.base.extensions

import android.content.res.TypedArray
import net.kozhanov.battleship.base.core.data.TextResource

fun TypedArray.getTextResource(index: Int): TextResource = TextResource.fromText(getStringOrEmpty(index))

fun TypedArray.getStringOrEmpty(index: Int): String = if (hasValue(index)) {
    getString(index).orEmpty()
} else {
    ""
}

fun TypedArray.getDrawableIdOrNull(index: Int): Int? = if (hasValue(index)) {
    val resId = getResourceId(index, -1)
    if (resId != -1) resId else null
} else {
    null
}
