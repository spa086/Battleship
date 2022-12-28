package net.kozhanov.battleship.base.core.data

import arrow.core.Either
import net.kozhanov.battleship.base.core.data.models.GameState

interface GameRepository {
    suspend fun getGameState(): Either<Throwable, GameState>
}
