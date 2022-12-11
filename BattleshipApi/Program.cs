using System.Text.Json;

namespace BattleshipApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        var pool = new GamePool();
        app.MapGet("/start", _ => Task.Run(() => JsonSerializer.Serialize(pool.StartGame())));
        app.MapGet("/whatsUp", _ => Task.Run(() => JsonSerializer.Serialize(pool.WhatsUp())));
        //todo tdd what if model is null
        MapPost<ShipFrontModel[]>(app, "createFleet", pool.CreateFleet); 
        //todo mb this one should be GET with parameters from query?
        MapPost<LocationTransportModel>(app, "attack", pool.Attack);
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

public class GamePool
{
    //true if game is started, false if we are waiting for second player to join.
    public bool StartGame() => throw new NotImplementedException();
    public WhatsUpResponse WhatsUp() => throw new NotImplementedException();
    public void CreateFleet(ShipFrontModel[] shipsToCreate) => throw new NotImplementedException();
    public void Attack(LocationTransportModel model) => throw new NotImplementedException();
}

public enum WhatsUpResponse
{
    WaitingForStart,
    CreatingShips,
    YourTurn,
    OpponentsTurn
}

