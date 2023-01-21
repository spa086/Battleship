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
        MapPostFunction<FleetCreationRequestModel, bool>(app, "createFleet", 
            (m, c) => c.CreateFleet(m));
        MapPostFunction<AttackRequestModel, AttackResponse>(app, "attack", (m, c) => c.Attack(m));
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
    public int userId { get; set; }

    public LocationModel location { get; set; }
}

public class WhatsupRequestModel
{
    public int userId { get; set; }
}

public class FleetCreationRequestModel
{
    public int userId { get; set; }

    public ShipTransportModel[] ships { get; set; } = Array.Empty<ShipTransportModel>();
}

public class LocationModel
{
    public int x { get; set; }
    public int y { get; set; }
}

public class ShipTransportModel
{
    public LocationModel[] decks { get; set; } = Array.Empty<LocationModel>();
}

public class GameAbortionRequestModel
{
    public int SessionId { get; set; }
}

public class Controller
{
    public GameStateModel WhatsUp(WhatsupRequestModel request)
    {
        var olgaIsPresent = GamePool.TheGame.FirstUserId.HasValue;
        var stasIsPresent = GamePool.TheGame.SecondUserId.HasValue;
        if(olgaIsPresent || stasIsPresent)
        {
            if (olgaIsPresent)
            {
                if (GamePool.TheGame.FirstUserId == request.userId)
                    return GameStateModel.YourTurn;
            }
            if (stasIsPresent)
            {
                if (GamePool.TheGame.SecondUserId == request.userId)
                    return GameStateModel.YourTurn;
            }
            return GameStateModel.OpponentsTurn;
        }

        var secondPlayerJoined = GamePool.StartPlaying(request.userId);
        if (secondPlayerJoined) return GameStateModel.CreatingFleet;
        else return GameStateModel.WaitingForStart;
    }

    public bool CreateFleet(FleetCreationRequestModel requestModel)
    {
        //todo tdd what if did not find game
        var game = GamePool.TheGame;
        var player1 = game.State == GameState.BothPlayersCreateFleets;
        game.CreateAndSaveShips(requestModel.userId, requestModel.ships.Select(ship =>
                new Ship { Decks = ship.decks.ToDictionary(x => ToCell(x),
                deckModel => new Deck(deckModel.x, deckModel.y))}).ToArray());
        return player1;
    }

    public Cell ToCell(LocationModel model)
    {
        return new Cell(model.x, model.y);
    }

    public AttackResponse Attack(AttackRequestModel model)
    {
        //todo tdd what if did not find game
        //todo check 3 times
        var attackResult = GamePool.TheGame.Attack(ToCell(model.location));
        var attackResultTransportModel = attackResult switch
        {
            AttackResult.Win => AttackResultTransportModel.Win,
            AttackResult.Killed => AttackResultTransportModel.Killed,
            AttackResult.Missed => AttackResultTransportModel.Missed,
            AttackResult.Hit => AttackResultTransportModel.Hit,
            _ => throw new Exception($"Unknown attack result [{attackResult}].")
        };
        var result = new AttackResponse { result = attackResultTransportModel };
        return result;
    } 
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
    public AttackResultTransportModel result { get; set; }
    //todo tdd filling
    public ShipStateModel[] fleet1 { get; set; } = Array.Empty<ShipStateModel>();
    //todo tdd filling
    public ShipStateModel[] fleet2 { get; set; } = Array.Empty<ShipStateModel>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackResultTransportModel
{
    Hit,
    Killed,
    Missed,
    Win
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameStateModel
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}