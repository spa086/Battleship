using BattleShipLibrary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

        MapPostFunction<WhatsupRequestModel, GameStateModel>(app, "whatsUp", (m, c) => c.WhatsUp(m));
        MapPostAction<FleetCreationRequestModel>(app, "createFleet", (m, c) => c.CreateFleet(m));
        MapPostFunction<AttackRequestModel, AttackResponse>(app, "attack", (m, c) => c.Attack(m));
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
    public GameStateModel WhatsUp(WhatsupRequestModel request)
    {
        var game = GamePool.TheGame;
        if(game is null)
        {
            GamePool.StartPlaying(request.userId);
            return GameStateModel.WaitingForStart;
        }
        if (game.SecondUserId is null)
        {
            GamePool.StartPlaying(request.userId);
            return GameStateModel.CreatingFleet;
        }
        if (game.FirstUserId is not null && game.SecondUserId is not null &&
            game.FirstFleet is not null && game.SecondFleet is not null)
            return RecognizeBattleStateModel(game, request.userId);
        return GameStateModel.CreatingFleet;
    }

    public bool CreateFleet(FleetCreationRequestModel requestModel)
    {
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
        //todo tdd what if did not find game
        //todo check 3 times
        var attackResult = GamePool.TheGame!.Attack(model.userId, ToCell(model.location));
        return new AttackResponse
        {
            result = attackResult switch
            {
                AttackResult.Win => AttackResultTransportModel.Win,
                AttackResult.Killed => AttackResultTransportModel.Killed,
                AttackResult.Missed => AttackResultTransportModel.Missed,
                AttackResult.Hit => AttackResultTransportModel.Hit,
                _ => throw new Exception($"Unknown attack result [{attackResult}].")
            }
        };
    }

    private static GameStateModel RecognizeBattleStateModel(Game game, int userId)
    {
        if (game.FirstUserId == userId)
            if (game.State == GameState.Player1Turn) return GameStateModel.YourTurn;
            else return GameStateModel.OpponentsTurn;
        else if (game.SecondUserId == userId)
            if (game.State == GameState.Player2Turn) return GameStateModel.YourTurn;
            else return GameStateModel.OpponentsTurn;
        //todo tdd
        else throw new Exception($"Unknown user id=[{userId}].");
    }
}
