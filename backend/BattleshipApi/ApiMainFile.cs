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

        MapPostFunction<WhatsupRequestModel, WhatsUpResponse>(app, "whatsUp", (m, c) => c.WhatsUp(m));
        MapPostFunction<FleetCreationRequestModel, bool>(app, "createFleet", 
            (m, c) => c.CreateFleet(m));
        MapPostFunction<AttackRequestModel, AttackResponse>(app, "attack", (m, c) => c.Attack(m));
        MapPostAction<GameAbortionRequestModel>(app, "abort", (m, c) => c.AbortGame(m));
        app.Run();
    }

    private static void MapPostAction<TRequestModel>(WebApplication app, string urlWithoutSlash,
        Action<TRequestModel, Controller> action)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            var requestModel = await GetRequestModel<TRequestModel>(context);
            action(requestModel, CreateController());
            Console.WriteLine("Successfully handled POST action request.");
        });
    }

    private static void MapPostFunction<TRequestModel, TResultModel>(WebApplication app, 
        string urlWithoutSlash, Func<TRequestModel, Controller, TResultModel> function)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            var requestModel = await GetRequestModel<TRequestModel>(context);
            var resultingModel = function(requestModel, CreateController());
            var resultingJson = JsonSerializer.Serialize(resultingModel); 
            Console.WriteLine($"Resulting JSON is: [{resultingJson}].");
            Console.WriteLine("Successfully handled POST function request.");
            return resultingJson;
        });
    }

    private static async Task<TRequestModel> GetRequestModel<TRequestModel>(HttpContext context)
    {
        var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        Console.WriteLine($"Input JSON: [{requestBody}].\n");
        var requestModel = JsonSerializer.Deserialize<TRequestModel>(requestBody);
        return requestModel!;
    }

    private static Controller CreateController() => new();
}

public class AttackRequestModel
{
    public int SessionId { get; set; }

    public int Location { get; set; }
}

public class WhatsupRequestModel
{
    public int SessionId { get; set; }

    public bool? IsFirstPlayer { get; set; }
}

public class FleetCreationRequestModel
{
    public int SessionId { get; set; }

    public ShipTransportModel[] Ships { get; set; } = Array.Empty<ShipTransportModel>();
}

public class ShipTransportModel
{
    public int[] Decks { get; set; } = Array.Empty<int>();
}

public class GameAbortionRequestModel
{
    public int SessionId { get; set; }
}

public class Controller
{
    public void AbortGame(GameAbortionRequestModel request) =>
        GamePool.Games.Remove(request.SessionId);

    public WhatsUpResponse WhatsUp(WhatsupRequestModel request)
    {
        //todo tdd did not find game
        if (GamePool.Games.TryGetValue(request.SessionId, out var game) &&
            game.State == GameState.Player1Turn)
            //todo tdd what if IsFirstPlayer is not set?
            if (request.IsFirstPlayer!.Value) return WhatsUpResponse.YourTurn;
            else return WhatsUpResponse.OpponentsTurn;
        var secondPlayerJoined = GamePool.StartPlaying(request.SessionId);
        if (secondPlayerJoined) return WhatsUpResponse.CreatingFleet;
        else return WhatsUpResponse.WaitingForStart;
    }

    public bool CreateFleet(FleetCreationRequestModel requestModel)
    {
        //todo tdd what if did not find game
        var game = GamePool.Games[requestModel.SessionId];
        var player1 = game.State == GameState.BothPlayersCreateFleets;
        game.CreateAndSaveShips(new FleetCreationModel
        {
            IsForPlayer1 = player1,
            Ships = requestModel.Ships.Select(ship =>
                new ShipCreationModel { Decks = ship.Decks.ToArray() }).ToArray()
        });
        return player1;
    }

    //todo tdd returned value
    public AttackResponse Attack(AttackRequestModel model)
    {
        //todo tdd what if did not find game
        //todo check 3 times
        var attackResult = GamePool.Games[model.SessionId].Attack(model.Location);
        var result = attackResult switch
        {
            AttackResult.Win => AttackResponse.Win,
            AttackResult.Killed => AttackResponse.Killed,
            AttackResult.Missed => AttackResponse.Missed,
            AttackResult.Hit => AttackResponse.Hit,
            _ => throw new Exception($"Unknown attack result [{attackResult}].")
        };
        return result;
    } 
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackResponse
{
    Hit,
    Killed,
    Missed,
    Win
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}