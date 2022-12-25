package net.kozhanov.battleship.base.core.data

import android.content.res.Resources
import androidx.annotation.PluralsRes
import androidx.annotation.StringRes

@Suppress("unused")
sealed class TextResource {
    companion object {
        fun fromText(text: String): TextResource = SimpleTextResource(text)
        fun fromStringId(@StringRes id: Int): TextResource = IdTextResource(id)
        fun fromFormattedStringId(@StringRes id: Int, value: String): TextResource = FormattedTextResource(id, value)
        fun fromFormattedStringId(@StringRes id: Int, value: Int): TextResource = FormattedTextResourceInt(id, value)
        fun fromPlural(@PluralsRes id: Int, pluralsValue: Int): TextResource = PluralsTextResource(id, pluralsValue)
        fun fromFormattedStringId(@StringRes id: Int, vararg value: TextResource): TextResource =
            FormattedTextResourceWithMultipleArguments(id, value)
    }
}

private data class SimpleTextResource(
    val text: String
) : TextResource()

private data class IdTextResource(
    @StringRes val id: Int
) : TextResource()

private data class FormattedTextResource(
    @StringRes val id: Int,
    val value: String,
) : TextResource()

private data class FormattedTextResourceInt(
    @StringRes val id: Int,
    val value: Int,
) : TextResource()

private data class PluralsTextResource(
    @PluralsRes val pluralId: Int,
    val quantity: Int
) : TextResource()

private class FormattedTextResourceWithMultipleArguments(
    @StringRes val id: Int,
    val values: Array<out TextResource>
) : TextResource()

@Suppress("SpreadOperator")
fun TextResource.asString(resources: Resources): String = when (this) {
    is SimpleTextResource -> this.text
    is IdTextResource -> resources.getString(this.id)
    is PluralsTextResource -> resources.getQuantityString(this.pluralId, this.quantity)
    is FormattedTextResource -> resources.getString(this.id, this.value)
    is FormattedTextResourceInt -> resources.getString(this.id, this.value)
    is FormattedTextResourceWithMultipleArguments -> resources.getString(
        this.id,
        *this.values.map {
            it.asString(resources)
        }.toTypedArray()
    )
}

fun String?.toTextResourceOrDefault(@StringRes default: Int): TextResource {
    return takeIf { !it.isNullOrBlank() }?.let { TextResource.fromText(it) } ?: TextResource.fromStringId(default)
}

fun String?.toTextResourceOrDefault(default: String): TextResource {
    return takeIf { !it.isNullOrBlank() }?.let { TextResource.fromText(it) } ?: TextResource.fromText(default)
}
