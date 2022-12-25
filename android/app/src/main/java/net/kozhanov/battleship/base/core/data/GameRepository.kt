package net.kozhanov.battleship.base.core.data

import arrow.core.Either

interface GameRepository {
    suspend fun createGame(): Either<Throwable, Unit>
}
