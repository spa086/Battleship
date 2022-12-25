package ru.openbank.accept.base.core.platform.interfaces.fragment

/**
 * Такие фрагменты содержат в себе кастомные действия при нажатии на кнопку Back
 * back() вызываем если childFragmentManager.fragments содержит Backable фрагмент
 */
interface Backable {
    fun back()
}
