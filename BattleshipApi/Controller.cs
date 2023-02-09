using BattleshipLibrary;

namespace BattleshipApi;
public class Controller
{
    public void AbortGame(int userId)
    {
        var game = GamePool.GetGame(userId);
        if (game is null)
        {
            //todo tdd throw here
        }
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
        GamePool.Games.Remove(game.Id);
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
    }

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        var userId = request.userId;
        var game = GamePool.GetGame(userId);
        if (game is null) return StartPlaying(userId);
        if (game.FirstUserId.HasValue && !game.SecondUserId.HasValue) return WaitingForStartResult();
        if (game.FirstUserId.HasValue && game.SecondUserId.HasValue &&
            (game.FirstFleet is null || game.SecondFleet is null))
            return WhatsUpWhileCreatingFleets(game);
        if (game.FirstFleet is not null && game.SecondFleet is not null)
            return WhatsUpInBattle(request, game);
        //todo tdd this exception
        throw new Exception("Unknown situation.");
    }

    private static WhatsUpResponseModel WhatsUpWhileCreatingFleets(Game game) =>
        new()
        {
            gameState = GameStateModel.CreatingFleet,
            myFleet = ToFleetStateModel(game.FirstFleet),
            opponentFleet = ToFleetStateModel(game.SecondFleet)
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
        var result = new WhatsUpResponseModel
        {
            gameState = stateModel!.Value,
            myExcludedLocations = myExcludedLocations,
            opponentExcludedLocations = opponentExcludedLocations,
            myFleet = myFleet,
            opponentFleet = opponentFleet,
        };
        return result;
    }

    private static GameStateModel? GetStateModel(WhatsupRequestModel request, Game game)
    {
        GameStateModel? result = null;
        if (game.State == GameState.Player1Turn && game.FirstUserId == request.userId ||
                    game.State == GameState.Player2Turn && game.SecondUserId == request.userId)
            result = GameStateModel.YourTurn;
        if (game.State == GameState.Player1Turn && game.SecondUserId == request.userId ||
            game.State == GameState.Player2Turn && game.FirstUserId == request.userId)
            result = GameStateModel.OpponentsTurn;
        return result;
    }

    private static WhatsUpResponseModel WaitingForStartResult() =>
        new() { gameState = GameStateModel.WaitingForStart };

    private static WhatsUpResponseModel StartPlaying(int userId) => new()
    {
        gameState = GamePool.StartPlaying(userId) ? GameStateModel.CreatingFleet
                : GameStateModel.WaitingForStart
    };

    public bool CreateFleet(FleetCreationRequestModel request)
    {
        if (request.ships.Any(x => x.decks is null))
            throw new Exception("Empty decks are not allowed.");
        var firstGroupWithDuplicates = request.ships.SelectMany(x => x.decks)
            .GroupBy(deck => new Cell(deck.x, deck.y))
            .FirstOrDefault(x => x.Count() > 1);
        if (firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: [{firstGroupWithDuplicates.Key}].");
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
        //todo tdd throw if game is inappropriate state
        //todo tdd what if did not find game
        //todo check 3 times
        var game = GamePool.GetGame(request.userId);
        var attackResult = game!.Attack(request.userId, ToCell(request.location));
        return new AttackResponse
        {
            result = ToAttackResultModel(attackResult),
            //todo tdd throw if first fleet is null
            fleet1 = ToFleetStateModel(game.FirstFleet)!,
            //todo tdd throw if first fleet is null
            fleet2 = ToFleetStateModel(game.SecondFleet)!,
            excludedLocations1 = game.ExcludedLocations1.Select(ToLocationModel).ToArray(),
            excludedLocations2 = game.ExcludedLocations2.Select(ToLocationModel).ToArray()
        };
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
}