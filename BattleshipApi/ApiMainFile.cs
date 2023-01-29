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
    //todo tdd use the userId param to check if the user is authorized to abort TheGame
#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public void AbortGame(int userId) => GamePool.SetGame(null);
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр

    public WhatsUpResponseModel WhatsUp(WhatsupRequestModel request)
    {
        //todo throw if it's third player
        var game = GamePool.TheGame;
        if(game is null)
        {
            GamePool.StartPlaying(request.userId);
            return GenerateWhatsupResponse(GameStateModel.WaitingForStart);
        }
        if (game.SecondUserId is null) return AwaitingSecondPlayerSituation(request, game);
        if (game.FirstUserId is not null && game.SecondUserId is not null &&
            game.FirstFleet is not null && game.SecondFleet is not null)
            return ProcessWhatsUpInBattle(request, game);
        return GenerateWhatsupResponse(GameStateModel.CreatingFleet);
    }

    public bool CreateFleet(FleetCreationRequestModel requestModel)
    {
        if (requestModel.ships.Any(x => x.decks is null)) 
            throw new Exception("Empty decks are not allowed.");
        var firstGroupWithDuplicates = requestModel.ships.SelectMany(x => x.decks)
            .GroupBy(deck => new Cell(deck.x, deck.y))
            .FirstOrDefault(x => x.Count() > 1);
        if(firstGroupWithDuplicates is not null)
            throw new Exception($"Two decks are at the same place: [{firstGroupWithDuplicates.Key}].");
        //todo tdd what if did not find game
        var game = GamePool.TheGame!;
        game.CreateAndSaveShips(requestModel.userId, 
            requestModel.ships.Select(ship =>
                new Ship { Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))}).ToArray());
        return game.FirstUserId == requestModel.userId;
    }

    public Cell ToCell(LocationModel model) => new(model.x, model.y);

    public AttackResponse Attack(AttackRequestModel model)
    {
        //todo tdd throw if game is inappropriate state
        //todo tdd what if did not find game
        //todo check 3 times
        var attackResult = GamePool.TheGame!.Attack(model.userId, ToCell(model.location));
        return new AttackResponse
        {
            result = ToAttackResultModel(attackResult),
            //todo tdd throw if first fleet is null
            fleet1 = ToFleetStateModel(GamePool.TheGame.FirstFleet!),
            //todo tdd throw if first fleet is null
            fleet2 = ToFleetStateModel(GamePool.TheGame.SecondFleet!)
        };
    }

    private static AttackResultTransportModel ToAttackResultModel(AttackResult attackResult)
    {
        return attackResult switch
        {
            AttackResult.Win => AttackResultTransportModel.Win,
            AttackResult.Killed => AttackResultTransportModel.Killed,
            AttackResult.Missed => AttackResultTransportModel.Missed,
            AttackResult.Hit => AttackResultTransportModel.Hit,
            _ => throw new Exception($"Unknown attack result [{attackResult}].")
        };
    }

    private static WhatsUpResponseModel RecognizeBattleStateModel(Game game, int userId)
    {
        if (game.FirstUserId == userId)
            if (game.State == GameState.Player1Turn) 
                return GenerateWhatsupResponse(GameStateModel.YourTurn);
            else return GenerateWhatsupResponse(GameStateModel.OpponentsTurn);
        else if (game.SecondUserId == userId)
            if (game.State == GameState.Player2Turn) 
                return GenerateWhatsupResponse(GameStateModel.YourTurn);
            else return GenerateWhatsupResponse(GameStateModel.OpponentsTurn);
        //todo tdd
        else throw new Exception($"Unknown user id=[{userId}].");
    }

    //todo mb kill 2 params?
    private static WhatsUpResponseModel GenerateWhatsupResponse(GameStateModel stateModel,
        IEnumerable<LocationModel> excludedLocations1 = null, 
        IEnumerable<LocationModel> excludedLocations2 = null)
    {
        return new() 
        {
            gameState = stateModel, excludedLocations1 = excludedLocations1?.ToArray(), 
            excludedLocations2 = excludedLocations2?.ToArray()
        };
    }

    private static WhatsUpResponseModel ProcessWhatsUpInBattle(WhatsupRequestModel request, Game game)
    {
        var result = RecognizeBattleStateModel(game, request.userId);
        result.fleet1 = ToFleetStateModel(game.FirstFleet!); //todo tdd handle null
        result.fleet2 = ToFleetStateModel(game.SecondFleet!); //todo tdd handle null
        result.excludedLocations1 = GamePool.TheGame.ExcludedLocations1
            .Select(cell => new LocationModel { x = cell.x, y = cell.y }).ToArray();
        result.excludedLocations2 = GamePool.TheGame.ExcludedLocations2
            .Select(cell => new LocationModel { x = cell.x, y = cell.y }).ToArray();
        return result;
    }

    private static ShipStateModel[] ToFleetStateModel(IEnumerable<Ship> fleet) => 
        fleet.Select(ship =>
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

    private static WhatsUpResponseModel AwaitingSecondPlayerSituation(WhatsupRequestModel request,
        Game game)
    {
        if (request.userId == game.FirstUserId)
            return GenerateWhatsupResponse(GameStateModel.WaitingForStart);
        else
        {
            GamePool.StartPlaying(request.userId);
            return GenerateWhatsupResponse(GameStateModel.CreatingFleet);
        }
    }
}
