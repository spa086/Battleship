using BattleshipLibrary;
using System.Text.Json;

namespace BattleshipApi;

public static class MainApi
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
        MapPostFunction<WhatsupRequestModel, WhatsUpResponseModel>(app, "whatsUp", 
            (model, controller) => controller.WhatsUp(model));
        MapPostFunction<AttackRequestModel, AttackResponse>(app, "attack", 
            (model, controller) => controller.Attack(model));
        MapPostAction<FleetCreationRequestModel>(app, "createFleet", 
            (model, controller) => controller.CreateFleet(model));
        MapPostAction<int>(app, "abortGame", (userId, controller) => controller.AbortGame(userId));
        app.Run();
    }

    private static void MapPostAction<TRequestModel>(WebApplication app, 
        string urlWithoutSlash, Action<TRequestModel, Controller> action)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            action(await GetRequestModel<TRequestModel>(context), CreateController());
            Console.WriteLine("Successfully handled POST action request.");
        });
    }

    private static void MapPostFunction<TRequestModel, TResultModel>(
        WebApplication app, string urlWithoutSlash, 
        Func<TRequestModel, Controller, TResultModel> function)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            Console.WriteLine($"Starting to process POST request by URL [{urlWithoutSlash}]...");
            var resultingJson = JsonSerializer.Serialize(function(
                await GetRequestModel<TRequestModel>(context), CreateController())); 
            Console.WriteLine($"Resulting JSON is: [{resultingJson}].");
            Console.WriteLine("Successfully handled POST function request.");
            return resultingJson;
        });
    }

    private static async Task<TRequestModel> GetRequestModel<TRequestModel>(HttpContext context)
    {
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Input JSON: [{requestBody}].\n");
        return JsonSerializer.Deserialize<TRequestModel>(requestBody)!;
    }

    private static Controller CreateController() => new();
}
