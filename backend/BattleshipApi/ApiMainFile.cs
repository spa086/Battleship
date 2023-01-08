using BattleShipLibrary;
using System.Text;
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
        MapPostAction<ShipFrontModel[]>(app, "createFleet", (m, c) => c.CreateFleet(m));
        MapPostAction<LocationTransportModel>(app, "attack", (m, c) => c.Attack(m));
        app.Run();
    }

    private static void MapPostAction<TRequestModel>(WebApplication app, string urlWithoutSlash,
        Action<TRequestModel, Controller> action)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            var requestModel = await GetRequestModel<TRequestModel>(context);
            action(requestModel, CreateController()); ////todo tdd what if model is null
            Console.WriteLine($"Successfully handled POST action request.");
        });
    }

    private static void MapPostFunction<TRequestModel, TResultModel>(WebApplication app, string urlWithoutSlash,
        Func<TRequestModel, Controller, TResultModel> function = null)
    {

        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            var requestModel = await GetRequestModel<TRequestModel>(context);
            var resultingModel = function(requestModel, CreateController());
            var resultingJson = JsonSerializer.Serialize(resultingModel); ////todo tdd what if model is null
            Console.WriteLine($"Resulting JSON is: [{resultingJson}].");
            Console.WriteLine($"Successfully handled POST function request.");
            return resultingJson;
        });
    }

    private static async Task<TRequestModel> GetRequestModel<TRequestModel>(HttpContext context)
    {
        var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        Console.WriteLine($"Input JSON: [{requestBody}].\n");
        var requestModel = JsonSerializer.Deserialize<TRequestModel>(requestBody);
        return requestModel;
    }

    private static Controller CreateController() => new();
}

public class LocationTransportModel
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class WhatsupRequestModel
{
    public int SessionId { get; set; }
}

public class ShipFrontModel
{
    public int[] Decks { get; set; } = Array.Empty<int>();
}

public class Controller
{
    //todo abort game

    public WhatsUpResponse WhatsUp(WhatsupRequestModel request)
    {
        //todo tdd check for null smh
        if (GamePool.TheGame?.Started ?? false) return WhatsUpResponse.CreatingFleet;
        GamePool.StartPlaying(request.SessionId);
        return WhatsUpResponse.WaitingForStart;
    }

    public void CreateFleet(ShipFrontModel[] shipsToCreate)
    {
        //todo tdd what if game is null
        GamePool.TheGame!.CreateAndSaveShips(new FleetCreationModel
        {
            IsForPlayer1 = true,
            Ships = shipsToCreate.Select(ship => 
                new ShipCreationModel { Decks = ship.Decks.ToArray() }).ToArray()
        });
    }

    public void Attack(LocationTransportModel model) => throw new NotImplementedException();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}