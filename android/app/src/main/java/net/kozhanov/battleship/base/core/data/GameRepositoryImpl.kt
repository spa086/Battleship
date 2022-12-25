package net.kozhanov.battleship.base.core.data

import arrow.core.Either
import arrow.core.maybe
import net.kozhanov.battleship.base.core.data.models.GameApi
import ru.openbank.accept.base.extensions.attempt

class GameRepositoryImpl(private val gameApi: GameApi) : GameRepository {
    override suspend fun createGame(): Either<Throwable, Unit> {
        return attempt {
            gameApi.getForecast()
        }
    }
}