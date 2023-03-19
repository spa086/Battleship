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
            await ActionHandler(urlWithoutSlash, action, context);
        });
    }

    private static async Task ActionHandler<TRequestModel>(string urlWithoutSlash,
        Action<TRequestModel, Controller> action, HttpContext context)
    {
        Log.Info($"Starting to process POST request by URL [{urlWithoutSlash}]...");
        try
        {
            var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
            action(WebResult.GetRequestModel<TRequestModel>(json), new Controller());
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            throw;
        }
        Log.Info("Successfully handled POST action request.");
    }

    private static void MapPostFunction<TRequestModel, TResultModel>(
        WebApplication app, string urlWithoutSlash,
        Func<TRequestModel, Controller, TResultModel> function) =>
        app.MapPost($"/{urlWithoutSlash}",
            async delegate (HttpContext context)
            {
                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                return WebResult.Prepare(urlWithoutSlash, function, json);
            });
}

public static class WebResult
{
    public static string Prepare<TRequestModel, TResultModel>(
        string urlWithoutSlash, Func<TRequestModel, Controller, TResultModel> function,
        string requestBody)
    {
        Log.Info($"Starting to process POST request by URL [{urlWithoutSlash}]...");
        var result = GetResultWithLogging(function, requestBody);
        var resultingJson = JsonSerializer.Serialize(result);
        Log.Info($"Resulting JSON is: [{resultingJson}].");
        Log.Info("Successfully handled POST function request.");
        return resultingJson;
    }

    public static TRequestModel GetRequestModel<TRequestModel>(string json)
    {
        Log.Info($"Input JSON: [{json}].\n");
        return JsonSerializer.Deserialize<TRequestModel>(json)!;
    }

    private static object GetResultWithLogging<TRequestModel, TResultModel>(
        Func<TRequestModel, Controller, TResultModel> function, string requestBody)
    {
        try
        {
            return function(GetRequestModel<TRequestModel>(requestBody), new Controller())!;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return ex.Message;
        }
    }
}