package net.kozhanov.battleship.features.board

import android.Manifest.permission
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import net.kozhanov.battleship.base.core.data.GameRepository
import net.kozhanov.battleship.base.core.platform.BaseViewModel
import net.kozhanov.battleship.base.core.platform.ErrorEvent
import net.kozhanov.battleship.base.core.platform.Event
import net.kozhanov.battleship.features.board.BoardDataEvent.OnNewText
import net.kozhanov.battleship.features.board.BoardErrorEvent.OnConnectError
import net.kozhanov.battleship.features.board.BoardUIEvent.StartGame
import net.kozhanov.battleship.features.board.BoardViewState.State
import net.kozhanov.battleship.features.board.BoardViewState.State.Loading
import net.kozhanov.battleship.features.board.BoardViewState.State.Result
import retrofit2.HttpException
import ru.openbank.accept.base.extensions.fold

class BoardViewModel(private val gameRepository: GameRepository) : BaseViewModel<BoardViewState>() {
    override fun initialViewState() = BoardViewState()

    init {
        start()
    }

    private fun start() {
        viewModelScope.launch(Dispatchers.IO) {
            gameRepository.createGame().fold(onSuccess = {
                processDataEvent(OnNewText("ты пидор(ка)"))
            }, onError = {
                processErrorEvent(OnConnectError(it))
            })
        }
    }

    override fun reduce(event: Event) = when (event) {
        is BoardDataEvent -> dispatchDataEvent(event)
        is BoardUIEvent -> dispatchUIEvent(event)
        else -> previousState
    }

    private fun dispatchUIEvent(event: BoardUIEvent) = when (event) {
        StartGame -> {
            start()
            previousState.copy(state = Loading)
        }
    }

    private fun dispatchDataEvent(event: BoardDataEvent) = when (event) {
        is OnNewText -> previousState.copy(state = Result(event.text, ""))
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
                        "Ошибка сети",
                        "${event.error.cause}"
                    )
                )
            }
        }
        else -> previousState
    }

}