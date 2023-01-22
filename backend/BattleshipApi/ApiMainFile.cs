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

#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены
    private static void MapPostAction<TRequestModel>(WebApplication app, string urlWithoutSlash,
#pragma warning restore IDE0051 // Удалите неиспользуемые закрытые члены
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
#pragma warning disable IDE1006 // Стили именования
    public int userId { get; set; }
    public LocationModel location { get; set; } = new LocationModel();
#pragma warning restore IDE1006 // Стили именования
}

public class WhatsupRequestModel
{
#pragma warning disable IDE1006 // Стили именования
    public int userId { get; set; }
#pragma warning restore IDE1006 // Стили именования
}

public class FleetCreationRequestModel
{
#pragma warning disable IDE1006 // Стили именования
    public int userId { get; set; }
#pragma warning restore IDE1006 // Стили именования

#pragma warning disable IDE1006 // Стили именования
    public ShipTransportModel[] ships { get; set; } = Array.Empty<ShipTransportModel>();
#pragma warning restore IDE1006 // Стили именования
}

public class LocationModel
{
    public int x { get; set; }
    public int y { get; set; }
}

public class ShipTransportModel
{
#pragma warning disable IDE1006 // Стили именования
    public LocationModel[] decks { get; set; } = Array.Empty<LocationModel>();
#pragma warning restore IDE1006 // Стили именования
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
        if (game.FirstUserId is not null &&
            game.SecondUserId is not null &&
            game.FirstFleet is not null &&
            game.SecondFleet is not null)
        {
            if(game.FirstUserId == request.userId)
            {
                if (game.State == GameState.Player1Turn)
                    return GameStateModel.YourTurn;
                else return GameStateModel.OpponentsTurn;
            }
            else if (game.SecondUserId == request.userId)
            {
                if (game.State == GameState.Player2Turn)
                    return GameStateModel.YourTurn;
                else return GameStateModel.OpponentsTurn;
            }
            else
            {
                //todo tdd
                throw new Exception($"Unknown user id=[{request.userId}].");
            }
        }
        return GameStateModel.CreatingFleet;
    }

    public bool CreateFleet(FleetCreationRequestModel requestModel)
    {
        //todo tdd what if did not find game
        var game = GamePool.TheGame!;
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
        var attackResult = GamePool.TheGame!.Attack(model.userId, ToCell(model.location));
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
#pragma warning disable IDE1006 // Стили именования
    public int x { get; set; }
#pragma warning restore IDE1006 // Стили именования
#pragma warning disable IDE1006 // Стили именования
    public int y { get; set; }
#pragma warning restore IDE1006 // Стили именования
#pragma warning disable IDE1006 // Стили именования
    public bool destroyed { get; set; }
#pragma warning restore IDE1006 // Стили именования
}

public class ShipStateModel
{
#pragma warning disable IDE1006 // Стили именования
    public DeckStateModel[] decks { get; set; } = Array.Empty<DeckStateModel>();
#pragma warning restore IDE1006 // Стили именования
}

public class AttackResponse
{
#pragma warning disable IDE1006 // Стили именования
    public AttackResultTransportModel result { get; set; }
#pragma warning restore IDE1006 // Стили именования
    //todo tdd filling
#pragma warning disable IDE1006 // Стили именования
    public ShipStateModel[] fleet1 { get; set; } = Array.Empty<ShipStateModel>();
#pragma warning restore IDE1006 // Стили именования
    //todo tdd filling
#pragma warning disable IDE1006 // Стили именования
    public ShipStateModel[] fleet2 { get; set; } = Array.Empty<ShipStateModel>();
#pragma warning restore IDE1006 // Стили именования
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