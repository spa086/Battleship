package net.kozhanov.battleship.base.extensions

private const val DEFAULT_SEPARATOR = ""
private const val HEX_PLACEHOLDER = "%02x"
fun ByteArray.toHex(separator: String = DEFAULT_SEPARATOR): String = joinToString(separator = separator) { byte ->
    HEX_PLACEHOLDER.format(byte)
}
