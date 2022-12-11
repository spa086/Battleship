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
        app.MapGet("/whatsup", _ => Task.Run(() => JsonSerializer.Serialize(pool.WhatsUp())));
        app.Run();
    }
}

public class GamePool
{
    //true if game is started, false if we are waiting for second player to join.
    public bool StartGame() => throw new NotImplementedException();
    //true if its requester's turn, false if we are waiting for the game to start
    public bool WhatsUp() => throw new NotImplementedException();
}

