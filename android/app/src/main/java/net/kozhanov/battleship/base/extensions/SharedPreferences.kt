package net.kozhanov.battleship.base.extensions

import android.content.SharedPreferences

inline fun <reified T : Any?> SharedPreferences.get(key: String): T? {
    when (T::class) {
        Boolean::class -> return if (this.contains(key)) this.getBoolean(key, false) as T else null
        Float::class -> return if (this.contains(key)) this.getFloat(key, 0f) as T else null
        Int::class -> return if (this.contains(key)) this.getInt(key, 0) as T else null
        Long::class -> return if (this.contains(key)) this.getLong(key, 0) as T else null
        String::class -> return if (this.contains(key)) this.getString(key, null) as T else null
        Set::class -> return if (this.contains(key)) this.getStringSet(key, null) as T else null
    }

    return null
}

@Suppress("UNCHECKED_CAST")
inline fun <reified T : Any?> SharedPreferences.put(key: String, value: T?) {
    val editor = this.edit()

    if (value == null) {
        editor.remove(key)
    } else {
        when (T::class) {
            Boolean::class -> editor.putBoolean(key, value as Boolean)
            Float::class -> editor.putFloat(key, value as Float)
            Int::class -> editor.putInt(key, value as Int)
            Long::class -> editor.putLong(key, value as Long)
            String::class -> editor.putString(key, value as String)
            else -> {
                if (value is Set<*>) {
                    editor.putStringSet(key, value as Set<String>)
                }
            }
        }
    }

    editor.apply()
}
