package net.kozhanov.battleship.base.core.data

import arrow.core.Either
import net.kozhanov.battleship.base.core.data.models.GameApi
import net.kozhanov.battleship.base.core.data.models.GameState
import ru.openbank.accept.base.extensions.attempt
import kotlin.random.Random

class GameRepositoryImpl(private val gameApi: GameApi) : GameRepository {
    private val userId = 0
    override suspend fun getGameState(): Either<Throwable, GameState> {
        return attempt {
            gameApi.getGameState(userId)
        }
    }
}