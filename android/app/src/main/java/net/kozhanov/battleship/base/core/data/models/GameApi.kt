package net.kozhanov.battleship.base.core.data.models

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface GameApi {
    @POST("/whatsup")
    suspend fun getGameState(@Body id: Int): GameState
}