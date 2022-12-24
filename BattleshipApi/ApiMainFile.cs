using System.Text.Json;
using BattleShipLibrary;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        builder.Services.AddSingleton<GamePool>();
        app.MapGet("/start", (GamePool pool) => Task.Run(() => JsonSerializer.Serialize(new Controller(pool).StartGame())));
        app.MapGet("/whatsUp", (GamePool pool) => Task.Run(() => JsonSerializer.Serialize(new Controller(pool).WhatsUp())));
        //todo tdd what if model is null
        MapPost<ShipFrontModel[]>(app, "createFleet", (GamePool pool) => new Controller(pool).CreateFleet()); 
        //todo mb this one should be GET with parameters from query?
        MapPost<LocationTransportModel>(app, "attack", (GamePool pool) => new Controller(pool).Attack());
        app.Run();
    }

    private static void MapPost<RequestModelType>(WebApplication app, string url, 
        Action<RequestModelType> action) =>
        app.MapPost("/" + url, context => Task.Run(() =>
        {
            var reader = new StreamReader(context.Request.Body);
            var requestJson = reader.ReadToEnd();
            var model = JsonSerializer.Deserialize<RequestModelType>(requestJson);
            action(model!); //todo tdd what if model is null
        }));
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
    public Controller(GamePool pool)
    {
        this.pool = pool;
    }

    //true if game is started, false if we are waiting for second player to join.
    public bool StartGame() => true;
    public WhatsUpResponse WhatsUp() => throw new NotImplementedException();
    public void CreateFleet(ShipFrontModel[] shipsToCreate) => throw new NotImplementedException();
    public void Attack(LocationTransportModel model) => throw new NotImplementedException();
    private GamePool pool;
}

public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingShips,
    YourTurn,
    OpponentsTurn
}