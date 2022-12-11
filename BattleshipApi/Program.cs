using System.Text.Json;

namespace BattleshipApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapGet("/start", _ => Task.Run(() => new GamePool().StartGame()));
        app.Run();
    }
}

public class GamePool
{
    public void StartGame() => throw new NotImplementedException();
}


