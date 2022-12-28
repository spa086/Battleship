package net.kozhanov.battleship.features.board

import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import net.kozhanov.battleship.base.core.data.GameRepository
import net.kozhanov.battleship.base.core.data.models.GameState.*
import net.kozhanov.battleship.base.core.platform.BaseViewModel
import net.kozhanov.battleship.base.core.platform.ErrorEvent
import net.kozhanov.battleship.base.core.platform.Event
import net.kozhanov.battleship.features.board.BoardDataEvent.OnNewText
import net.kozhanov.battleship.features.board.BoardDataEvent.RefreshGameState
import net.kozhanov.battleship.features.board.BoardErrorEvent.OnConnectError
import net.kozhanov.battleship.features.board.BoardUIEvent.StartGame
import net.kozhanov.battleship.features.board.BoardViewState.State.Loading
import net.kozhanov.battleship.features.board.BoardViewState.State.Result
import retrofit2.HttpException
import ru.openbank.accept.base.extensions.fold

class BoardViewModel(private val gameRepository: GameRepository) : BaseViewModel<BoardViewState>() {
    override fun initialViewState() = BoardViewState()

    private suspend fun refreshGameState() {
        gameRepository.getGameState().fold(onSuccess = {
            when (it) {
                WaitingForStart -> {

                }
                CreatingShips -> {
                    processDataEvent(OnNewText("Build you own ship, destroy the enemy!"))
                }
                YourTurn -> {
                    processDataEvent(OnNewText("You turn"))
                }
                OpponentsTurn -> {
                    processDataEvent(OnNewText("Waiting for opponent turn"))
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
            processDataEvent(RefreshGameState)
            previousState.copy(state = Loading)
        }
    }

    private fun dispatchDataEvent(event: BoardDataEvent) = when (event) {
        is OnNewText -> previousState.copy(state = Result(event.text, ""))
        RefreshGameState -> {
            viewModelScope.launch {
                refreshGameState()
            }
            previousState
        }
    }

    override fun onHandleErrorEvent(event: ErrorEvent) = when (event) {
        is OnConnectError -> {
            val ev = event.error
            if (ev is HttpException) {
                previousState.copy(
                    state = Result(
                        "Ошибка Сервера",
                        "${ev.code()}, ${ev.message}"
                    )
                )
            } else {
                previousState.copy(
                    state = Result(
                        "Неизвестная ошибка",
                        "${event.error.message}"
                    )
                )
            }
        }
        else -> previousState
    }

}