using System.Text.Json;

namespace BattleshipApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        var gamePool = new GamePool();
        app.MapGet("/start", _ => Task.Run(() => gamePool.StartGame()));

        app.Run();
    }
}

public class GamePool
{
    public void StartGame() => throw new NotImplementedException();
}


