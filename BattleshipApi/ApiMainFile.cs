using BattleshipLibrary;
using System.Text.Json;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
        MapPostFunction<WhatsupRequestModel, WhatsUpResponseModel>(app, "whatsUp", 
            (model, controller) => controller.WhatsUp(model));
        MapPostFunction<AttackRequestModel, AttackResponse>(app, "attack", 
            (model, controller) => controller.Attack(model));
        MapPostAction<FleetCreationRequestModel>(app, "createFleet", 
            (model, controller) => controller.CreateFleet(model));
        MapPostAction<int>(app, "abortGame", (userId, controller) => controller.AbortGame(userId));
        app.Run();
    }

    private static void MapPostAction<TRequestModel>(WebApplication app, 
        string urlWithoutSlash, Action<TRequestModel, Controller> action)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            action(await GetRequestModel<TRequestModel>(context), CreateController());
            Console.WriteLine("Successfully handled POST action request.");
        });
    }

    private static void MapPostFunction<TRequestModel, TResultModel>(
        WebApplication app, string urlWithoutSlash, 
        Func<TRequestModel, Controller, TResultModel> function)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            var resultingJson = JsonSerializer.Serialize(function(
                await GetRequestModel<TRequestModel>(context), CreateController())); 
            Console.WriteLine($"Resulting JSON is: [{resultingJson}].");
            Console.WriteLine("Successfully handled POST function request.");
            return resultingJson;
        });
    }

    private static async Task<TRequestModel> GetRequestModel<TRequestModel>(HttpContext context)
    {
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Input JSON: [{requestBody}].\n");
        return JsonSerializer.Deserialize<TRequestModel>(requestBody)!;
    }

    private static Controller CreateController() => new();
}

public class Controller
{
    public void AbortGame(int userId)
    {
        var game = GamePool.GetGame(userId);
        if (game is null)
        {
            //todo tdd throw here
        }
#pragma warning disable CS8602 // –азыменование веро€тной пустой ссылки.
        GamePool.Games.Remove(game.Id);
#pragma warning restore CS8602 // –азыменование веро€тной пустой ссылки.
    }

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        var userId = request.userId;
        var game = GamePool.GetGame(userId);
        if(game is null) return StartPlaying(userId);
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
            gameState = GameStateModel.CreatingFleet, fleet1 = ToFleetStateModel(game.FirstFleet),
            fleet2 = ToFleetStateModel(game.SecondFleet)
        };

    private static WhatsUpResponseModel WhatsUpInBattle(WhatsupRequestModel request, Game game)
    {
        var excludedLocations1 = game.ExcludedLocations1.Select(ToLocationModel).ToArray();
        var excludedLocations2 = game.ExcludedLocations2.Select(ToLocationModel).ToArray();
        var firstFleet = ToFleetStateModel(game.FirstFleet);
        var secondFleet = ToFleetStateModel(game.SecondFleet);
        GameStateModel? stateModel = null;
        if (game.State == GameState.Player1Turn && game.FirstUserId == request.userId ||
            game.State == GameState.Player2Turn && game.SecondUserId == request.userId)
            stateModel = GameStateModel.YourTurn;
        if (game.State == GameState.Player1Turn && game.SecondUserId == request.userId ||
            game.State == GameState.Player2Turn && game.FirstUserId == request.userId)
            stateModel = GameStateModel.OpponentsTurn;
        var result = new WhatsUpResponseModel
        {
            gameState = stateModel!.Value,excludedLocations1 = excludedLocations1,
            excludedLocations2 = excludedLocations2, fleet1 = firstFleet, fleet2 = secondFleet,
        };
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
        if(firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: [{firstGroupWithDuplicates.Key}].");
        //todo tdd what if did not find a game
        var game = GamePool.GetGame(request.userId);
        game!.CreateAndSaveShips(request.userId, 
            request.ships.Select(ship =>
                new Ship { Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))}).ToArray());
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
