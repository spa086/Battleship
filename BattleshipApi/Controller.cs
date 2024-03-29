﻿using BattleshipLibrary;

namespace BattleshipApi;

public class Controller
{
    private readonly GamePool gamePool;

    public Controller(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public NewGameResponseModel NewGame(NewGameRequestModel model) => StartPlaying(model.userId);

    public void AbortGame(int userId)
    {
        Log.ger.Info($"User id=[{userId}] wants to abort game.");
        var game = gamePool.GetGame(userId);
        if (game is not null && !game.ItsOver)
        {
            game.DisposeOfTimer();
            if (game.State == GameState.WaitingForGuest) game.Cancel();
            else game.SetTechnicalWinner(userId == game.Guest!.Id);
        }
        else Log.ger.Info($"Game for abortion not found by user id=[{userId}].");
    }

    public WhatsUpResponseModel WhatsUp(WhatsUpRequestModel request)
    {
        var userId = request.userId;
        var game = gamePool.GetGame(userId);
        if (game is null) return new WhatsUpResponseModel { gameState = GameStateModel.NoGame };
        WhatsUpResponseModel? result;
        if (game is { State: GameState.Cancelled, Guest: null } || game.State == GameState.WaitingForGuest)
            result = BasicResponseModel(
                game.State == GameState.Cancelled ? GameStateModel.Cancelled : GameStateModel.WaitingForStart);
        else if (game.Guest is not null && (game.Host.Fleet is null || game.Guest!.Fleet is null))
            result = WhatsUpBeforeBattle(userId, game);
        else if (game.Host.Fleet is not null && game.Guest!.Fleet is not null)
            result = WhatsUpInBattle(userId, game);
        else throw new Exception("Unknown situation.");
        result.userName = userId == game.Host.Id ? game.Guest?.Name : game.Host.Name;
        result.secondsLeft = game.TimerSecondsLeft;
        return result;
    }

    public bool CreateFleet(FleetCreationRequestModel request)
    {
        Log.ger.Info($"User [{request.userId}|{request.userName}] wants to create ships.");
        var game = gamePool.GetGame(request.userId);
        if (game is null)
            throw new Exception($"User [{request.userId}] does not participate in any ongoing games.");
        var firstGroupWithDuplicates =
            request.ships.SelectMany(x => x.decks)
                .GroupBy(deck => new Cell(deck.x, deck.y))
                .FirstOrDefault(x => x.Count() > 1);
        if (firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: {firstGroupWithDuplicates.Key}.");
        if (request.userId == game.Host.Id) game.Host.Name = request.userName;
        else if (request.userId == game.Guest!.Id) game.Guest!.Name = request.userName;
        game.SaveShips(request.userId, request.ships.Select(ToShip).ToArray());
        Log.ger.Info($"Ships were created for user [{request.userId}|{request.userName}].");
        return game.Host.Id == request.userId;
    }

    public AttackResponse Attack(AttackRequestModel request)
    {
        Log.ger.Info($"User id=[{request.userId}] wants to attack at " +
                     $"[{request.location.x},{request.location.y}].");
        var game = gamePool.GetGame(request.userId);
        if (game is null) throw new Exception($"Could not find game for user id=[{request.userId}].");
        var userName = game.State == GameState.HostTurn ? game.Host.Name : game.Guest!.Name;
        var attackResult = game.Attack(request.userId, ToCell(request.location));
        Log.ger.Info($"User id=[{request.userId}] performed attack at " +
                     $"[{request.location.x},{request.location.y}].");
        return new AttackResponse
        {
            result = ToAttackResultModel(attackResult),
            excludedLocations1 = game.Host.ExcludedLocations.Select(ToLocationModel).ToArray(),
            excludedLocations2 = game.Guest!.ExcludedLocations.Select(ToLocationModel).ToArray(),
            opponentName = userName
        };
    }

    private static WhatsUpResponseModel WhatsUpBeforeBattle(int userId, Game game)
    {
        var isHost = userId == game.Host.Id;
        var myFleet = isHost ? game.Host.Fleet : game.Guest!.Fleet;
        var opponentFleet = isHost ? game.Guest!.Fleet : game.Host.Fleet;
        return new WhatsUpResponseModel(game.Id, GetStateModel(userId, game), ToFleetStateModel(myFleet),
            ToFleetStateModel(opponentFleet), null, null);
    }

    private static LocationModel[] GetMyExcludedLocations(Game game, bool forFirstUser) =>
        forFirstUser
            ? ToExcludedLocationModels(game.Host.ExcludedLocations)
            : ToExcludedLocationModels(game.Guest!.ExcludedLocations);

    private static LocationModel[] GetOpponentExcludedLocations(Game game, bool forHost) =>
        forHost
            ? ToExcludedLocationModels(game.Guest!.ExcludedLocations)
            : ToExcludedLocationModels(game.Host.ExcludedLocations);

    private static LocationModel[] ToExcludedLocationModels(List<Cell> locations) =>
        locations.Select(ToLocationModel).ToArray();

    private static WhatsUpResponseModel WhatsUpInBattle(int userId, Game game)
    {
        var forHost = userId == game.Host.Id;
        var myExcludedLocations = GetMyExcludedLocations(game, forHost);
        var opponentExcludedLocations = GetOpponentExcludedLocations(game, forHost);
        var myFleet = forHost
            ? ToFleetStateModel(game.Host.Fleet)
            : ToFleetStateModel(game.Guest!.Fleet);
        var opponentFleet = forHost
            ? ToFleetStateModel(game.Guest!.Fleet)
            : ToFleetStateModel(game.Host.Fleet);
        var stateModel = GetStateModel(userId, game);
        var result = new WhatsUpResponseModel(game.Id, stateModel, myFleet, opponentFleet,
            myExcludedLocations, opponentExcludedLocations);
        return result;
    }

    private static GameStateModel GetStateModel(int userId, Game game)
    {
        var isHost = game.Host.Id == userId;
        var isGuest = game.Guest!.Id == userId;
        if (game.State == GameState.BothPlayersCreateFleets || game.State == GameState.OnePlayerCreatesFleet)
            return GameStateModel.CreatingFleet;
        if (game.State == GameState.HostTurn && isHost || game.State == GameState.GuestTurn && isGuest)
            return GameStateModel.YourTurn;
        if (game.State == GameState.HostTurn && isGuest || game.State == GameState.GuestTurn && isHost)
            return GameStateModel.OpponentsTurn;
        if (game.State == GameState.HostWon && isHost || game.State == GameState.GuestWon && isGuest)
            return GameStateModel.YouWon;
        if (game.State == GameState.HostWon && isGuest || game.State == GameState.GuestWon && isHost)
            return GameStateModel.OpponentWon;
        if (game.State == GameState.Cancelled) return GameStateModel.Cancelled;
        throw new Exception($"Unknown situation. State = [{game.State}], " +
                            $"host id = [{game.Host.Id}], guest id = [{game.Guest!.Id}], " +
                            $"requester user id = [{userId}].");
    }

    private static WhatsUpResponseModel BasicResponseModel(GameStateModel state) => new() { gameState = state };

    private NewGameResponseModel StartPlaying(int userId)
    {
        var secondPlayerJoined = gamePool.StartPlaying(userId);
        var game = gamePool.GetGame(userId)!;
        var eventDescription = secondPlayerJoined ? "joined" : "started";
        Log.ger.Info($"User with id [{userId}] {eventDescription} a game with id [{game.Id}].");
        return new() { gameId = game.Id, secondsLeft = game.TimerSecondsLeft!.Value };
    }

    private static Cell ToCell(LocationModel model) => new(model.x, model.y);

    private static LocationModel ToLocationModel(Cell location) => new() { x = location.X, y = location.Y };

    private static AttackResultTransportModel ToAttackResultModel(AttackResult attackResult) =>
        attackResult switch
        {
            AttackResult.Win => AttackResultTransportModel.Win,
            AttackResult.Missed => AttackResultTransportModel.Missed,
            AttackResult.Hit => AttackResultTransportModel.Hit,
            _ => throw new Exception($"Unknown attack result [{attackResult}].")
        };

    private static ShipStateModel[]? ToFleetStateModel(IEnumerable<Ship>? fleet) =>
        fleet?.Select(ship =>
            new ShipStateModel
            {
                decks = ship.Decks.Select(deck =>
                    new DeckStateModel
                        { destroyed = deck.Value.Destroyed, x = deck.Key.X, y = deck.Key.Y }).ToArray()
            }).ToArray();

    private static Ship ToShip(ShipForCreationModel ship) =>
        new() { Decks = ship.decks.ToDictionary(ToCell, deckModel => new Deck(deckModel.x, deckModel.y)) };
}