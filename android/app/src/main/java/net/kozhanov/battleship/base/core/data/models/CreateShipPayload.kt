package net.kozhanov.battleship.base.core.data.models

data class CreateShipPayload(val userId: Int){
    data class NewShip(val deck: NewDeck) {
        data class NewDeck(val x: Int, val y: Int)
    }
}