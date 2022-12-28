using BattleShipLibrary;
using System.Text.Json;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        MapPost<WhatsupRequestModel>(app, "whatsUp", (m, c) => c. WhatsUp(m));
        //todo tdd what if model is null
        MapPost<ShipFrontModel[]>(app, "createFleet", (m, c) => c.CreateFleet(m));
        //todo mb this one should be GET with parameters from query?
        MapPost<LocationTransportModel>(app, "attack", (m, c) => c.Attack(m));
        app.Run();
    }

    private static void MapPost<RequestModelType>(WebApplication app, string url, 
        Action<RequestModelType, Controller> action) =>
        app.MapPost("/" + url, context => Task.Run(() =>
        {
            var reader = new StreamReader(context.Request.Body);
            var requestJson = reader.ReadToEnd();
            var model = JsonSerializer.Deserialize<RequestModelType>(requestJson);
            action(model!, CreateController()); //todo tdd what if model is null
        }));

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

public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}