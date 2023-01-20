package net.kozhanov.battleship.features.board

import net.kozhanov.battleship.base.core.data.models.Ship
import net.kozhanov.battleship.base.core.data.models.Ship.Deck
import net.kozhanov.battleship.base.core.platform.DataEvent
import net.kozhanov.battleship.base.core.platform.ErrorEvent
import net.kozhanov.battleship.base.core.platform.SingleEvent
import net.kozhanov.battleship.base.core.platform.UiEvent
import net.kozhanov.battleship.features.board.BoardViewState.State.*


data class BoardViewState(
    val state: State = State.Init
) {
    val isLoadingVisible: Boolean = state is State.Loading
    val isResultVisible: Boolean = state is State.Board
    val isTitleVisible: Boolean = state is Message
    val isSubTitleVisible: Boolean = state is Message
    val isBoardVisible: Boolean = state is CreatingShip || state is Board
    val isStartVisible: Boolean = state is Init
    val isNextShipVisible: Boolean = state is CreatingShip && state.isShipFull

    sealed class State {
        object Init : State()
        object Loading : State()
        data class CreatingShip(val decks: List<Deck>, val shipMaxSize: Int) : State() {
            val isShipFull = decks.size >= shipMaxSize
        }

        data class Board(val ships: List<Ship>) : State()
        data class Message(val title: String, val text: String) : State()
    }
}

sealed class BoardDataEvent : DataEvent {
    object OnRefreshState : BoardDataEvent()
    data class OnNewText(val text: String) : BoardDataEvent()
    object OnCreateShip : BoardDataEvent()
}

sealed class BoardErrorEvent : ErrorEvent {
    data class OnConnectError(override val error: Throwable) : BoardErrorEvent()
}

sealed class BoardUIEvent : UiEvent {
    object StartGame : BoardUIEvent()
    data class OnBoardTap(val x: Int, val y: Int) : BoardUIEvent()
}

sealed class BoardSingleEvent : SingleEvent {

}