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
            action(await GetRequestModel<TRequestModel>(context), CreateController());
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
        Func<TRequestModel, Controller, TResultModel> function)
    {
        app.MapPost($"/{urlWithoutSlash}", async delegate (HttpContext context)
        {
            return await FunctionHandler(urlWithoutSlash, function, context);
        });
    }

    private static async Task<string> FunctionHandler<TRequestModel, TResultModel>(
        string urlWithoutSlash, Func<TRequestModel, Controller, TResultModel> function,
        HttpContext context)
    {
        Log.Info($"Starting to process POST request by URL [{urlWithoutSlash}]...");
        var resultModel = await GetResultWithLogging(function, context);
        var resultingJson = JsonSerializer.Serialize(resultModel);
        Log.Info($"Resulting JSON is: [{resultingJson}].");
        Log.Info("Successfully handled POST function request.");
        return resultingJson;
    }

    private static async Task<TResultModel> GetResultWithLogging<TRequestModel, TResultModel>(
        Func<TRequestModel, Controller, TResultModel> function, HttpContext context)
    {
        TResultModel? result;
        try
        {
            result = function(await GetRequestModel<TRequestModel>(context),
                CreateController());
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            throw;
        }
        return result;
    }

    private static async Task<TRequestModel> GetRequestModel<TRequestModel>(HttpContext context)
    {
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Log.Info($"Input JSON: [{requestBody}].\n");
        return JsonSerializer.Deserialize<TRequestModel>(requestBody)!;
    }

    private static Controller CreateController() => new();
}