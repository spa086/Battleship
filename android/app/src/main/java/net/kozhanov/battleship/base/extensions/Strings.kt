package net.kozhanov.battleship.base.extensions

import java.util.Locale

fun String.capitalizeFirstLetter() =
    replaceFirstChar { if (it.isLowerCase()) it.titlecase(Locale.getDefault()) else it.toString() }

private const val DEFAULT_LIMIT_FOR_DIVIDE = 3
private const val OFFSET_FOR_PENNY = 3

/**
 * Преобразует денежные суммы в читаемый формат (с учетом копеек):
 * 1000 -> 1 000
 * 1000,10 -> 1 000,10
 * 100000 -> 100 000
 * 1000000,99 -> 1 000 000,00
 */
fun String.divideBy(withPenny: Boolean, limit: Int = DEFAULT_LIMIT_FOR_DIVIDE): String {
    if (this.isEmpty()) return this
    var result = ""
    val offset = if (withPenny) OFFSET_FOR_PENNY else 0 // Если есть копейки, то цикл движется до запятой
    for ((index, i) in (this.lastIndex - offset downTo 0).withIndex()) {
        // на каждый limit'тный индекс добавляем пробел
        result = "${this[i]}${ if (index % limit == 0 && index > 0) " " else "" }$result"
    }
    return "$result${this.takeLast(offset)}" // добавляем копейки, если надо
}
