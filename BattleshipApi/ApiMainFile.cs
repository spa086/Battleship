using BattleShipLibrary;
using System.Text.Json;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        MapGet(app, "/start", c => c.StartGame());
        MapGet(app, "/whatsUp", c => c.WhatsUp());
        //todo tdd what if model is null
        MapPost<ShipFrontModel[]>(app, "createFleet", (m, c) => c.CreateFleet(m));
        //todo mb this one should be GET with parameters from query?
        MapPost<LocationTransportModel>(app, "attack", (m, x) => x.Attack(m));
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

    private static void MapGet<T>(WebApplication app, string url, Func<Controller, T> action) =>
        app.MapGet(url, () => Task.Run(() => JsonSerializer
            .Serialize(action(CreateController()))));

    private static Controller CreateController() => new();
}

public class LocationTransportModel
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class ShipFrontModel
{
    public int[] Decks { get; set; } = Array.Empty<int>();
}

public class Controller
{
    //true if game is started, false if we are waiting for second player to join.
    public bool StartGame() => GamePool.StartPlaying();
    
    public WhatsUpResponse WhatsUp()
    {
        //todo tdd check for null smh
        if (GamePool.TheGame!.Started)
            return WhatsUpResponse.CreatingFleet;
        return WhatsUpResponse.WaitingForStart;
    }

    public void CreateFleet(ShipFrontModel[] shipsToCreate) => throw new NotImplementedException();
    public void Attack(LocationTransportModel model) => throw new NotImplementedException();
}

public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingFleet,
    YourTurn,
    OpponentsTurn
}