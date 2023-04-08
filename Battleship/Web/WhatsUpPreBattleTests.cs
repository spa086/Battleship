using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Web;

public class WhatsUpPreBattleTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    private readonly TestRandomFleet testRandomFleet;

    public WhatsUpPreBattleTests()
    {
        //todo 3 times
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddTransient<Controller>();
        services.AddSingleton<IRandomFleet, TestRandomFleet>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
        testRandomFleet = (serviceProvider.GetService<IRandomFleet>() as TestRandomFleet)!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [TestCase(GameState.GuestWon)]
    [TestCase(GameState.HostWon)]
    public void GameInVictoryStateButWithoutShips(GameState state)
    {
        var game = testingEnvironment.CreateNewTestableGame(state, 1, 2, false);
        game.SetupFleets(null, null);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        var expectedState =
            state == GameState.HostWon ? GameStateModel.YouWon : GameStateModel.OpponentWon;
        Assert.That(result.gameState, Is.EqualTo(expectedState));
    }

    [Test]
    public void CancelledGame()
    {
        testingEnvironment.CreateNewTestableGame(GameState.Cancelled, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.Cancelled));
    }

    [Test]
    public void SecondPlayerCreatesFleetFirst()
    {
        testingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet,
            hostHasFleet: false);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(2));

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
        Assert.That(result.secondsLeft, Is.EqualTo(60));
    }

    //todo make Host-Guest enum??

    [TestCase(GameState.OnePlayerCreatesFleet, true)]
    [TestCase(GameState.OnePlayerCreatesFleet, false)]
    [TestCase(GameState.BothPlayersCreateFleets, true)]
    [TestCase(GameState.BothPlayersCreateFleets, false)]
    public void WhatsUpWhileCreatingShips(GameState state, bool forHost)
    {
        var game = testingEnvironment.CreateNewTestableGame(state, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(forHost ? 1 : 2));

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
        Assert.That(result.gameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void HostPlayerWhatsupWhileWaitingForGuest()
    {
        testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 1);

        var result = CallWhatsupViaController(1);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest,
            1, 2);

        var result = CallWhatsupViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.Guest!.Id, Is.EqualTo(2));
    }

    [Test]
    public void FirstPlayerStarts()
    {
        testRandomFleet.SetupAiShips = Array.Empty<Ship>();

        var result = controller.WhatsUp(new WhatsupRequestModel { userId = 1 });

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
        var gameId = gamePool.Games.Keys.AssertSingle();
        Assert.That(result.gameId, Is.EqualTo(gameId));
    }

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) =>
        new() { userId = userIdParam };

    private WhatsUpResponseModel CallWhatsupViaController(int userId) =>
        controller.WhatsUp(CreateWhatsUpRequestModel(userId));
}
