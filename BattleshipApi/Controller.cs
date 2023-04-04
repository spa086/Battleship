using BattleshipLibrary;

namespace BattleshipApi;

public class Controller
{
    private readonly GamePool gamePool;

    public Controller(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public void AbortGame(int userId)
    {
        Log.ger.Info($"User id=[{userId}] wants to abort game.");
        var game = gamePool.GetGame(userId);
        if (game is not null)
        {
            game.SetTechnicalWinner(game.SecondUserId == userId);
            Log.ger.Info($"Game id=[{game.Id}] is removed.");
        }
        else Log.ger.Info($"Game for abortion not found by user id=[{userId}].");
    }

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        var userId = request.userId;
        var game = gamePool.GetGame(userId);
        WhatsUpResponseModel? result;
        if (game is null) (result, game) = (StartPlaying(userId), gamePool.GetGame(userId)!);
        else if (game.SecondUserId is null) result = WaitingForStartResult();
        else if (game.SecondUserId.HasValue &&
            (game.FirstFleet is null || game.SecondFleet is null))
            result = WhatsUpWhileCreatingFleets(game);
        else if (game.FirstFleet is not null && game.SecondFleet is not null)
            result = WhatsUpInBattle(request, game);
        //todo tdd this exception
        else throw new Exception("Unknown situation.");
        result.userName = 
            request.userId == game!.FirstUserId ? game.SecondUserName : game.FirstUserName; 
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
        if (request.userId == game!.FirstUserId) game.FirstUserName = request.userName;
        else if (request.userId == game.SecondUserId) game.SecondUserName = request.userName;
        game!.CreateAndSaveShips(request.userId, request.ships.Select(ToShip).ToArray());
        Log.ger.Info($"Ships were created for user [{request.userId}|{request.userName}].");
        return game.FirstUserId == request.userId;
    }

    public AttackResponse Attack(AttackRequestModel request)
    {
        Log.ger.Info($"User id=[{request.userId}] wants to attack at " +
            $"[{request.location.x},{request.location.y}].");
        //todo tdd throw if game is in inappropriate state
        //todo tdd what if did not find game
        var game = gamePool.GetGame(request.userId)!;
        AssertYourTurn(request, game);
        var attackResult = game!.Attack(request.userId, ToCell(request.location));
        Log.ger.Info($"User id=[{request.userId}] performed attack at " +
            $"[{request.location.x},{request.location.y}].");
        return new AttackResponse
        {
            result = ToAttackResultModel(attackResult),
            excludedLocations1 = game.ExcludedLocations1.Select(ToLocationModel).ToArray(),
            excludedLocations2 = game.ExcludedLocations2.Select(ToLocationModel).ToArray()
        };
    }

    private static WhatsUpResponseModel WhatsUpWhileCreatingFleets(Game game) =>
        new()
        {
            gameState = GameStateModel.CreatingFleet,
            myFleet = ToFleetStateModel(game.FirstFleet),
            opponentFleet = ToFleetStateModel(game.SecondFleet),
            gameId = game.Id
        };

    private static LocationModel[] GetMyExcludedLocations(Game game, bool forFirstUser) =>
        forFirstUser
            ? game.ExcludedLocations1.Select(ToLocationModel).ToArray()
            : game.ExcludedLocations2.Select(ToLocationModel).ToArray();

    private static LocationModel[] GetOpponentExcludedLocations(Game game, bool forFirstUser) =>
        forFirstUser
            ? game.ExcludedLocations2.Select(ToLocationModel).ToArray()
            : game.ExcludedLocations1.Select(ToLocationModel).ToArray();

    private static WhatsUpResponseModel WhatsUpInBattle(WhatsupRequestModel request, Game game)
    {
        var forFirstUser = request.userId == game.FirstUserId;
        var myExcludedLocations = GetMyExcludedLocations(game, forFirstUser);
        var opponentExcludedLocations = GetOpponentExcludedLocations(game, forFirstUser);
        var myFleet = forFirstUser ? ToFleetStateModel(game.FirstFleet)
            : ToFleetStateModel(game.SecondFleet);
        var opponentFleet = forFirstUser ? ToFleetStateModel(game.SecondFleet)
            : ToFleetStateModel(game.FirstFleet);
        GameStateModel? stateModel = GetStateModel(request, game);
        var result = new WhatsUpResponseModel(game.Id, stateModel!.Value, myFleet, opponentFleet,
            myExcludedLocations, opponentExcludedLocations, game.TurnSecondsLeft);
        return result;
    }

    private static GameStateModel GetStateModel(WhatsupRequestModel request, Game game)
    {
        if (game.State == GameState.Player1Turn && game.FirstUserId == request.userId ||
            game.State == GameState.Player2Turn && game.SecondUserId == request.userId)
            return GameStateModel.YourTurn;
        if (game.State == GameState.Player1Turn && game.SecondUserId == request.userId ||
            game.State == GameState.Player2Turn && game.FirstUserId == request.userId)
            return GameStateModel.OpponentsTurn;
        if (game.State == GameState.Player1Won && game.FirstUserId == request.userId ||
            game.State == GameState.Player2Won && game.SecondUserId == request.userId)
            return GameStateModel.YouWon;
        if (game.State == GameState.Player1Won && game.SecondUserId == request.userId ||
            game.State == GameState.Player2Won && game.FirstUserId == request.userId)
            return GameStateModel.OpponentWon;
        throw new Exception($"Unknown situation. State = [{game.State}], " +
            $"user 1 id = [{game.FirstUserId}], user 2 id = [{game.SecondUserId}], " +
            $"requester user id = [{request.userId}].");
    }

    private static WhatsUpResponseModel WaitingForStartResult() =>
        new() { gameState = GameStateModel.WaitingForStart };

    private WhatsUpResponseModel StartPlaying(int userId)
    {
        var secondPlayerJoined = gamePool.StartPlaying(userId);
        var game = gamePool.GetGame(userId)!;
        var eventDescription = secondPlayerJoined ? "joined" : "started";
        Log.ger.Info($"User with id [{userId}] {eventDescription} a game with id [{game.Id}].");
        return new()
        {
            gameState = secondPlayerJoined ? GameStateModel.CreatingFleet
                : GameStateModel.WaitingForStart,
            gameId = game.Id
        };
    }

    private static void AssertYourTurn(AttackRequestModel request, Game game)
    {
        if (game.State == GameState.Player1Turn && game.SecondUserId == request.userId ||
                    game.State == GameState.Player2Turn && game.FirstUserId == request.userId)
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
                {
                    destroyed = deck.Value.Destroyed,
                    x = deck.Key.x,
                    y = deck.Key.y
                }).ToArray()
            }).ToArray();

    private static Ship ToShip(ShipForCreationModel ship) =>
        new()
        {
            Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))
        };
}