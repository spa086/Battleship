using System.Text.Json.Serialization;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace BattleshipApi;

#pragma warning disable IDE1006 // Стили именования

public class NewGameRequestModel
{
    public int userId { get; set; }
}

public class NewGameResponseModel
{
    public int secondsLeft { get; set; }
    public int gameId { get; set; }
}

public class AttackRequestModel
{
    public int userId { get; set; }
    public LocationModel location { get; set; } = new();
}

public class WhatsUpResponseModel
{
    public WhatsUpResponseModel() { }

    public WhatsUpResponseModel(int gameId, GameStateModel gameState, ShipStateModel[]? myFleet, 
        ShipStateModel[]? opponentFleet, LocationModel[]? myExcludedLocations,
        LocationModel[]? opponentExcludedLocations)
    {
        this.gameState = gameState;
        this.myFleet = myFleet;
        this.opponentFleet = opponentFleet;
        this.myExcludedLocations = myExcludedLocations;
        this.opponentExcludedLocations = opponentExcludedLocations;
        this.gameId = gameId;
    }

    public string? userName { get; set; }
    public int? gameId { get; set; }
    public GameStateModel gameState { get; set; }
    public ShipStateModel[]? myFleet { get; set; }
    public ShipStateModel[]? opponentFleet { get; set; }
    public LocationModel[]? myExcludedLocations { get; set; }
    public LocationModel[]? opponentExcludedLocations { get; set; }
    public int? secondsLeft { get; set; }
}

public class WhatsUpRequestModel
{
    public int userId { get; set; }
}

public class FleetCreationRequestModel
{
    public int userId { get; set; }

    public string? userName { get; set; }

    public ShipForCreationModel[] ships { get; set; } = Array.Empty<ShipForCreationModel>();
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
    public string? opponentName { get; set; }
    public AttackResultTransportModel result { get; set; }
    //todo RETURN FLEETS
    public LocationModel[] excludedLocations1 { get; set; } = Array.Empty<LocationModel>();
    public LocationModel[] excludedLocations2 { get; set; } = Array.Empty<LocationModel>();
}


#pragma warning restore IDE1006 // Стили именования



[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackResultTransportModel
{
    Hit,
    Missed,
    Win
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameStateModel
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn,
    YouWon,
    OpponentWon,
    Cancelled,
    NoGame
}
