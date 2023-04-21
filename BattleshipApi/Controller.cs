using BattleshipLibrary;

namespace BattleshipApi;

public class Controller
{
    private readonly GamePool gamePool;

    public Controller(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public NewGameResponseModel NewGame(NewGameRequestModel model)
    {
        return StartPlaying(model.userId);
    }

    public void AbortGame(int userId)
    {
        Log.ger.Info($"User id=[{userId}] wants to abort game.");
        var game = gamePool.GetGame(userId);
        if (game is not null)
        {
            game.DisposeOfTimer();
            gamePool.Games.Remove(game.Id);
            Log.ger.Info($"Game id=[{game.Id}] is removed.");
        }
        else Log.ger.Info($"Game for abortion not found by user id=[{userId}].");
    }

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        var userId = request.userId;
        var game = gamePool.GetGame(userId);
        if (game is null)
            return new WhatsUpResponseModel
            {
                gameState = GameStateModel.NoGame,
            }; WhatsUpResponseModel? result;
        if (game.Guest is null) result = WaitingForStartResult();
        else if (game.Guest is not null && (game.Host!.Fleet is null || game.Guest!.Fleet is null))
            result = WhatsUpBeforeBattle(userId, game);
        else if (game.Host!.Fleet is not null && game.Guest!.Fleet is not null)
            result = WhatsUpInBattle(userId, game);
        //todo tdd this exception
        else throw new Exception("Unknown situation.");
        result.userName = userId == game?.Host?.Id ? game.Guest?.Name : game?.Host.Name;
        result.secondsLeft = game?.TimerSecondsLeft;
        return result;
    }

    public bool CreateFleet(FleetCreationRequestModel request)
    {
        Log.ger.Info($"User [{request.userId}|{request.userName}] wants to create ships.");
        if (request.ships.Any(x => x.decks is null))
            throw new Exception("Empty decks are not allowed.");
        var firstGroupWithDuplicates = request.ships.SelectMany(x => x.decks)
            .GroupBy(deck => new Cell(deck.x, deck.y))
            .FirstOrDefault(x => x.Count() > 1);
        if (firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: {firstGroupWithDuplicates.Key}.");
        //todo tdd what if did not find a game
        var game = gamePool.GetGame(request.userId);
        if (request.userId == game!.Host!.Id) game.Host.Name = request.userName;
        else if (request.userId == game.Guest!.Id) game.Guest!.Name = request.userName;
        game!.CreateAndSaveShips(request.userId, request.ships.Select(ToShip).ToArray());
        Log.ger.Info($"Ships were created for user [{request.userId}|{request.userName}].");
        return game.Host.Id == request.userId;
    }

    public AttackResponse Attack(AttackRequestModel request)
    {
        Log.ger.Info($"User id=[{request.userId}] wants to attack at " +
            $"[{request.location.x},{request.location.y}].");
        //todo tdd throw if game is in inappropriate state
        //todo tdd what if did not find game
        var game = gamePool.GetGame(request.userId)!;
        var userName = game.State == GameState.HostTurn ? game.Host!.Name : game.Guest!.Name;
        AssertYourTurn(request, game);
        var attackResult = game!.Attack(request.userId, ToCell(request.location));
        Log.ger.Info($"User id=[{request.userId}] performed attack at " +
            $"[{request.location.x},{request.location.y}].");
        return new AttackResponse
        {
            result = ToAttackResultModel(attackResult),
            excludedLocations1 = game.Host!.ExcludedLocations.Select(ToLocationModel).ToArray(),
            excludedLocations2 = game.Guest!.ExcludedLocations.Select(ToLocationModel).ToArray(),
            opponentName = userName
        };
    }

    private static WhatsUpResponseModel WhatsUpBeforeBattle(int userId, Game game) => 
        new(game.Id, GetStateModel(userId, game), ToFleetStateModel(game.Host!.Fleet),
            ToFleetStateModel(game.Guest!.Fleet), null, null);

    private static LocationModel[] GetMyExcludedLocations(Game game, bool forFirstUser) =>
        forFirstUser ? ToExcludedLocationModels(game.Host!.ExcludedLocations) 
            : ToExcludedLocationModels(game.Guest!.ExcludedLocations);

    private static LocationModel[] GetOpponentExcludedLocations(Game game, bool forHost) =>
        forHost ? ToExcludedLocationModels(game.Guest!.ExcludedLocations) : 
            ToExcludedLocationModels(game.Host.ExcludedLocations);

    private static LocationModel[] ToExcludedLocationModels(List<Cell> locations) =>
        locations.Select(ToLocationModel).ToArray();

    private static WhatsUpResponseModel WhatsUpInBattle(int userId, Game game)
    {
        var forHost = userId == game.Host.Id;
        var myExcludedLocations = GetMyExcludedLocations(game, forHost);
        var opponentExcludedLocations = GetOpponentExcludedLocations(game, forHost);
        var myFleet = forHost ? ToFleetStateModel(game.Host.Fleet)
            : ToFleetStateModel(game.Guest!.Fleet);
        var opponentFleet = forHost ? ToFleetStateModel(game.Guest!.Fleet)
            : ToFleetStateModel(game.Host.Fleet);
        var stateModel = GetStateModel(userId, game);
        var result = new WhatsUpResponseModel(game.Id, stateModel, myFleet, opponentFleet,
            myExcludedLocations, opponentExcludedLocations);
        return result;
    }

    //todo refactor long method
    private static GameStateModel GetStateModel(int userId, Game game)
    {
        if (game.State == GameState.BothPlayersCreateFleets || game.State == GameState.OnePlayerCreatesFleet)
            return GameStateModel.CreatingFleet;
        if (game.State == GameState.HostTurn && game.Host.Id == userId ||
            game.State == GameState.GuestTurn && game.Guest!.Id == userId)
            return GameStateModel.YourTurn;
        if (game.State == GameState.HostTurn && game.Guest!.Id == userId ||
            game.State == GameState.GuestTurn && game.Host.Id == userId)
            return GameStateModel.OpponentsTurn;
        if (game.State == GameState.HostWon && game.Host.Id == userId ||
            game.State == GameState.GuestWon && game.Guest!.Id == userId)
            return GameStateModel.YouWon;
        if (game.State == GameState.HostWon && game.Guest!.Id == userId ||
            game.State == GameState.GuestWon && game.Host.Id == userId)
            return GameStateModel.OpponentWon;
        if (game.State == GameState.Cancelled) return GameStateModel.Cancelled;
        throw new Exception($"Unknown situation. State = [{game.State}], " +
            $"host id = [{game.Host.Id}], guest id = [{game.Guest!.Id}], " +
            $"requester user id = [{userId}].");
    }

    private static WhatsUpResponseModel WaitingForStartResult() =>
        new() { gameState = GameStateModel.WaitingForStart };

    private NewGameResponseModel StartPlaying(int userId)
    {
        var secondPlayerJoined = gamePool.StartPlaying(userId);
        var game = gamePool.GetGame(userId)!;
        var eventDescription = secondPlayerJoined ? "joined" : "started";
        Log.ger.Info($"User with id [{userId}] {eventDescription} a game with id [{game.Id}].");
        return new()
        {
            gameId = game.Id,
            secondsLeft = game.TimerSecondsLeft!.Value
        };
    }

    private static void AssertYourTurn(AttackRequestModel request, Game game)
    {
        if (game.State == GameState.HostTurn && game.Guest!.Id == request.userId ||
                    game.State == GameState.GuestTurn && game.Host!.Id == request.userId)
            throw new Exception("Not your turn.");
    }

    private static Cell ToCell(LocationModel model) => new(model.x, model.y);

    private static LocationModel ToLocationModel(Cell location) =>
        new() { x = location.x, y = location.y };

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
                        { destroyed = deck.Value.Destroyed, x = deck.Key.x, y = deck.Key.y }).ToArray()
            }).ToArray();

    private static Ship ToShip(ShipForCreationModel ship) =>
        new()
        {
            Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))
        };
}