package net.kozhanov.battleship.features.board

import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch
import net.kozhanov.battleship.base.core.data.GameRepository
import net.kozhanov.battleship.base.core.data.models.GameState
import net.kozhanov.battleship.base.core.data.models.Ship.Deck
import net.kozhanov.battleship.base.core.platform.BaseViewModel
import net.kozhanov.battleship.base.core.platform.ErrorEvent
import net.kozhanov.battleship.base.core.platform.Event
import net.kozhanov.battleship.features.board.BoardDataEvent.*
import net.kozhanov.battleship.features.board.BoardErrorEvent.OnConnectError
import net.kozhanov.battleship.features.board.BoardUIEvent.OnBoardTap
import net.kozhanov.battleship.features.board.BoardUIEvent.StartGame
import net.kozhanov.battleship.features.board.BoardViewState.State.*
import net.kozhanov.battleship.features.board.BoardViewState.State.CreatingShip
import retrofit2.HttpException
import ru.openbank.accept.base.extensions.fold

class BoardViewModel(private val gameRepository: GameRepository) : BaseViewModel<BoardViewState>() {
    override fun initialViewState() = BoardViewState()

    private suspend fun refreshGameState() {
        gameRepository.getGameState().fold(onSuccess = {
            when (it) {
                GameState.WaitingForStart -> {
                    processDataEvent(OnCreateShip)
                }
                GameState.YourTurn -> {
                    processDataEvent(OnNewText("You turn"))
                }
                GameState.OpponentsTurn -> {
                    processDataEvent(OnNewText("Waiting for opponent turn"))
                }
                GameState.CreatingFleet -> {
                    processDataEvent(OnNewText("Build you own ship, destroy the enemy!"))
                }
            }
        }, onError = {
            processErrorEvent(OnConnectError(it))
        })
    }

    override fun reduce(event: Event) = when (event) {
        is BoardDataEvent -> dispatchDataEvent(event)
        is BoardUIEvent -> dispatchUIEvent(event)
        else -> previousState
    }

    private fun dispatchUIEvent(event: BoardUIEvent) = when (event) {
        StartGame -> {
            //processDataEvent(OnRefreshState)
            //previousState.copy(state = Loading)
            processDataEvent(OnCreateShip)
            previousState
        }
        is OnBoardTap -> when (val currState = previousState.state) {
            is Board -> previousState
            Init -> previousState
            Loading -> previousState
            is Message -> previousState
            is CreatingShip -> {
                if (currState.isShipFull.not()) {
                    val newDecks = currState.decks.toMutableList()
                    newDecks.add(Deck(event.x, event.y, false))
                    previousState.copy(state = currState.copy(decks = newDecks))
                } else {
                    previousState
                }
            }
        }
    }

    private fun dispatchDataEvent(event: BoardDataEvent) = when (event) {
        is OnNewText -> previousState.copy(state = Message("", event.text))
        OnRefreshState -> {
            viewModelScope.launch {
                refreshGameState()
            }
            previousState
        }
        OnCreateShip -> {
            
            previousState.copy(state = CreatingShip(emptyList(), 3))
        }
    }

    override fun onHandleErrorEvent(event: ErrorEvent) = when (event) {
        is OnConnectError -> {
            val ev = event.error
            if (ev is HttpException) {
                previousState.copy(
                    state = Message(
                        "Ошибка Сервера",
                        "${ev.code()}, ${ev.message}"
                    )
                )
            } else {
                previousState.copy(
                    state = Message(
                        "Неизвестная ошибка",
                        "${event.error.message}"
                    )
                )
            }
        }
        else -> previousState
    }

}