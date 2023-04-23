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
    private readonly TestAi testRandomFleet;

    public WhatsUpPreBattleTests()
    {
        var serviceProvider = TestServiceCollection.Web().BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
        testRandomFleet = (serviceProvider.GetService<IAi>() as TestAi)!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void WhatsUpOnCancelledGame()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.Cancelled, 33);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(33));
        
        Assert.That(result.gameState, Is.EqualTo(GameStateModel.Cancelled));
    }

    [Test]
    public void ReturningMatchingTimerSecondsWhenWaiting()
    {
        testingEnvironment.CreateNewTestableGame(
            GameState.WaitingForGuest, 1, matchingSeconds: 100);

        var result = controller.WhatsUp(new WhatsUpRequestModel { userId = 1 });

        Assert.That(result.secondsLeft, Is.EqualTo(100));
    }

    [Test]
    public void ReturningMatchingTimerSeconds()
    {
        testRandomFleet.SetupAiShips = Array.Empty<Ship>();
        gamePool.SetupMatchingTimeSeconds = 100;
        
        var result = controller.NewGame(new NewGameRequestModel { userId = 1 });

        Assert.That(result.secondsLeft, Is.EqualTo(100));
    }

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
        testingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet, 1, 2,
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

        var result = CallWhatsUpViaController(1);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest,
            1);

        controller.NewGame(new NewGameRequestModel { userId = 2 });

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.Guest!.Id, Is.EqualTo(2));
    }

    [Test]
    public void GameStart()
    {
        testRandomFleet.SetupAiShips = Array.Empty<Ship>();

        var result = controller.NewGame(new NewGameRequestModel { userId = 1 });

        var gameId = gamePool.Games.Keys.AssertSingle();
        Assert.That(result.gameId, Is.EqualTo(gameId));
    }

    private static WhatsUpRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) =>
        new() { userId = userIdParam };

    private WhatsUpResponseModel CallWhatsUpViaController(int userId) =>
        controller.WhatsUp(CreateWhatsUpRequestModel(userId));
}
