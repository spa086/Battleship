package net.kozhanov.battleship.base.core.data.models

data class Ship(val decks: List<Deck>) {
    data class Deck(val x: Int, val y: Int, val isDestroyed: Boolean)
}