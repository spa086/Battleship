package net.kozhanov.battleship.features.board

import net.kozhanov.battleship.base.core.platform.DataEvent
import net.kozhanov.battleship.base.core.platform.ErrorEvent
import net.kozhanov.battleship.base.core.platform.SingleEvent
import net.kozhanov.battleship.base.core.platform.UiEvent


data class BoardViewState(
    val state: State = State.Init
) {
    val isLoadingVisible: Boolean = state is State.Loading
    val isResultVisible: Boolean = state is State.Result

    sealed class State {
        object Init : State()
        object Loading : State()
        data class Result(val text: String, val subtitle: String) : State()
    }
}

sealed class BoardDataEvent : DataEvent {
    object RefreshGameState : BoardDataEvent()
    data class OnNewText(val text: String) : BoardDataEvent()
}

sealed class BoardErrorEvent : ErrorEvent {
    data class OnConnectError(override val error: Throwable) : BoardErrorEvent()
}

sealed class BoardUIEvent : UiEvent {
    object StartGame:BoardUIEvent()
}

sealed class BoardSingleEvent : SingleEvent {

}