package ru.openbank.accept.base.extensions

fun <T : Any> T?.wrapInList(): List<T> = if (this == null || this is String && this.isEmpty()) {
    emptyList()
} else {
    listOf(this)
}
