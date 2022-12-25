package net.kozhanov.battleship.base.extensions

import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Date

private const val MAX_LENGTH_SEC_TIMESTAMP = 10
private const val TIMESTAMP_FACTOR = 1000L
private const val ONLY_DATE_PATTERN = "dd.MM.yyyy"
val dtfOnlyDate: DateTimeFormatter by lazy { DateTimeFormatter.ofPattern(ONLY_DATE_PATTERN) }

fun Long.checkUnixTimeStamp() = if (this.toString().length <= MAX_LENGTH_SEC_TIMESTAMP) {
    this * TIMESTAMP_FACTOR
} else {
    this
}

fun Date.format(format: DateTimeFormatter): String {
    return time.parseTimeStamp().format(format)
}

fun Long.parseTimeStamp(): LocalDateTime =
    LocalDateTime.ofInstant(Instant.ofEpochMilli(this.checkUnixTimeStamp()), ZoneId.systemDefault())
