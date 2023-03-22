using BattleshipLibrary;

namespace BattleshipApi;

public class Controller
{
    public void AbortGame(int userId)
    {
        var game = GamePool.GetGame(userId);
        if (game is not null) GamePool.Games.Remove(game.Id);
    }

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        var userId = request.userId;
        var game = GamePool.GetGame(userId);
        if (game is not null) LogFleets(game);
        WhatsUpResponseModel? result;
        if (game is null) (result, game) = (StartPlaying(userId), GamePool.GetGame(userId)!);
        else if (game!.FirstUserId.HasValue && !game.SecondUserId.HasValue)
            result = WaitingForStartResult();
        else if (game.FirstUserId.HasValue && game.SecondUserId.HasValue &&
            (game.FirstFleet is null || game.SecondFleet is null))
            result = WhatsUpWhileCreatingFleets(game);
        else if (game.FirstFleet is not null && game.SecondFleet is not null)
            result = WhatsUpInBattle(request, game);
        //todo tdd this exception
        else throw new Exception("Unknown situation.");
        result.userName = 
            request.userId == game!.FirstUserId ? game.FirstUserName : game.SecondUserName;
        return result;
    }

    public bool CreateFleet(FleetCreationRequestModel request)
    {
        if (request.ships.Any(x => x.decks is null))
            throw new Exception("Empty decks are not allowed.");
        var firstGroupWithDuplicates = request.ships.SelectMany(x => x.decks)
            .GroupBy(deck => new Cell(deck.x, deck.y))
            .FirstOrDefault(x => x.Count() > 1);
        if (firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: {firstGroupWithDuplicates.Key}.");
        //todo tdd what if did not find a game
        var game = GamePool.GetGame(request.userId);
        game!.CreateAndSaveShips(request.userId,
            request.ships.Select(ship =>
                new Ship
                {
                    Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))
                }).ToArray());
        return game.FirstUserId == request.userId;
    }

    public AttackResponse Attack(AttackRequestModel request)
    {
        //todo tdd throw if game is in inappropriate state
        //todo tdd what if did not find game
        //todo check 3 times
        var game = GamePool.GetGame(request.userId)!;
        AssertYourTurn(request, game);
        var attackResult = game!.Attack(request.userId, ToCell(request.location));
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

    private static WhatsUpResponseModel StartPlaying(int userId)
    {
        var secondPlayerJoined = GamePool.StartPlaying(userId);
        return new()
        {
            gameState = secondPlayerJoined ? GameStateModel.CreatingFleet
                : GameStateModel.WaitingForStart,
            gameId = GamePool.GetGame(userId)!.Id
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
            AttackResult.Killed => AttackResultTransportModel.Killed,
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

    private static void LogFleets(Game? game)
    {
        var firstFleetStr = string.Join<Ship>(",", game!.FirstFleet ?? Array.Empty<Ship>());
        var secondFleetStr = string.Join<Ship>(",", game.SecondFleet ?? Array.Empty<Ship>());
        Log.Info($"Whatsup got game. First fleet = [{firstFleetStr}], " +
            $"second fleet = [{secondFleetStr}]. State = [{game.State}].");
    }
}