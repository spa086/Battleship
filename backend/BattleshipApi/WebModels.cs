﻿using BattleshipLibrary;
using System.Text.Json.Serialization;

namespace BattleshipApi;

#pragma warning disable IDE1006 // Стили именования

public class AttackRequestModel
{
    public int userId { get; set; }
    public LocationModel location { get; set; } = new LocationModel();
}

public class WhatsUpResponseModel
{
    public GameStateModel gameState { get; set; }
    //todo tdd filling
    public ShipStateModel[]? fleet1 { get; set; }
    //todo tdd filling
    public ShipStateModel[]? fleet2 { get; set; }
}

public class WhatsupRequestModel
{
    public int userId { get; set; }
}

public class FleetCreationRequestModel
{
    public int userId { get; set; }

    public ShipForCreationModel[] ships { get; set; }
        = Array.Empty<ShipForCreationModel>();
}

public class LocationModel
{
    public int x { get; set; }
    public int y { get; set; }
}

public class ShipForCreationModel
{
    public LocationModel[] decks { get; set; } = Array.Empty<LocationModel>();
}

public class DeckStateModel
{
    public int x { get; set; }
    public int y { get; set; }
    public bool destroyed { get; set; }
}

public class ShipStateModel
{
    public DeckStateModel[] decks { get; set; } = Array.Empty<DeckStateModel>();
}

public class AttackResponse
{
    public AttackResultTransportModel result { get; set; }
    //todo tdd filling
    public ShipStateModel[] fleet1 { get; set; } = Array.Empty<ShipStateModel>();
    //todo tdd filling
    public ShipStateModel[] fleet2 { get; set; } = Array.Empty<ShipStateModel>();
}


#pragma warning restore IDE1006 // Стили именования



[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackResultTransportModel
{
    Hit,
    Killed,
    Missed,
    Win
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameStateModel
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}
