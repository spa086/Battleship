namespace BattleShipBackend;

class Program
{
    public void Main()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        app.UseRouting();
        app.Run();
    }
}

