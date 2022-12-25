package net.kozhanov.battleship.base.core.data.models

import retrofit2.http.GET

interface GameApi {
    @GET("/whatsup")
    suspend fun getGameState(): Boolean
}