package net.kozhanov.battleship.base.core.platform

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import timber.log.Timber

/**
 * Одиночные ивенты: например для отображения диалога
 */
interface SingleEvent

/**
 * Ивенты, с которыми работает VM
 */
interface Event

/**
 * Ивенты, которые летят от View
 */
interface UiEvent : Event

/**
 * Ивенты, которые летят внутри VM
 */
interface DataEvent : Event

/**
 * Ивенты, которые летают между несколькими VM
 */
interface OutputEvent : Event

/**
 * Ивенты для обработки ошибок
 */
interface ErrorEvent : Event {
    val error: Throwable
}

@Suppress("TooManyFunctions")
abstract class BaseViewModel<VIEW_STATE> : ViewModel() {

    private val _viewState: MutableStateFlow<VIEW_STATE> by lazy { MutableStateFlow(initialViewState()) }
    val viewState: StateFlow<VIEW_STATE>
        get() = _viewState.asStateFlow()

    private val _singleEvent: Channel<SingleEvent> = Channel()
    val singleEvent = _singleEvent.receiveAsFlow()

    protected val previousState: VIEW_STATE
        get() = viewState.value ?: initialViewState()

    protected abstract fun initialViewState(): VIEW_STATE

    protected abstract fun reduce(event: Event): VIEW_STATE

    protected abstract fun onHandleErrorEvent(event: ErrorEvent): VIEW_STATE

    protected open fun onAfterStateChanged(newViewState: VIEW_STATE, event: Event) {
    }

    fun processUiEvent(event: UiEvent) {
        updateState(event)
    }

    protected fun processDataEvent(event: DataEvent) {
        updateState(event)
    }

    protected fun processOutputEvent(event: OutputEvent) {
        updateState(event)
    }

    protected fun sendSingleEvent(event: SingleEvent, actionAfter: () -> Unit = {}) {
        viewModelScope.launch {
            _singleEvent.send(event)
            actionAfter()
        }
    }

    private fun updateState(event: Event) {
        val newViewState = reduce(event)
        compareNewStateWithCurrentAndUpdate(newViewState, event)
    }

    protected fun processErrorEvent(errorEvent: ErrorEvent) {
        val newViewState = if (DefaultErrorDispatcher.processError(errorEvent.error)) {
            previousState
        } else {
            onHandleErrorEvent(event = errorEvent)
        }
        compareNewStateWithCurrentAndUpdate(newViewState, errorEvent)
    }

    private fun compareNewStateWithCurrentAndUpdate(newViewState: VIEW_STATE, event: Event) {
        if (newViewState != viewState.value) {
            _viewState.update { newViewState }
            onAfterStateChanged(newViewState, event)
        }
    }

    private object DefaultErrorDispatcher {

        fun processError(error: Throwable): Boolean {
            Timber.e(error)
            return false
        }
    }
}