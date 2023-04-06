using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests;

public class WhatsUpTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public WhatsUpTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddTransient<Controller>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void UserNameIsReturned()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2, true);
        game.SetupUserName(2, "Admiral");

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result.userName, Is.EqualTo("Admiral"));
    }

    [Test]
    public void GettingSecondsLeft()
    {
        testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result.secondsLeft, Is.EqualTo(30));
    }

    [Test]
    public void SecondPlayerCreatesFleetFirst()
    {
        testingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet, 
            firstPlayerHasFleet: false);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(2));
  
        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
    }

    [TestCase(GameState.HostWon, GameStateModel.YouWon)]
    [TestCase(GameState.GuestWon, GameStateModel.OpponentWon)]
    public void WhatsupWhenWon(GameState gameState, GameStateModel expectedModel)
    {
        testingEnvironment.CreateNewTestableGame(gameState, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result.gameState, Is.EqualTo(expectedModel));
    }

    [TestCase(GameState.OnePlayerCreatesFleet, true)]
    [TestCase(GameState.OnePlayerCreatesFleet, false)]
    [TestCase(GameState.BothPlayersCreateFleets, true)]
    [TestCase(GameState.BothPlayersCreateFleets, false)]
    public void WhatsUpWhileCreatingShips(GameState state, bool firstPlayer)
    {
        var game = testingEnvironment.CreateNewTestableGame(state, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(firstPlayer ? 1 : 2));

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
        Assert.That(result.gameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void OpponentExcludedLocations()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupExcludedLocations(1, new Cell(5, 6));

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(2));

        var location = result.opponentExcludedLocations.AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void MyExcludedLocations()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupExcludedLocations(1, new Cell(5, 6));

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        var location = result.myExcludedLocations.AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void FirstPlayerWhatsupWhileWaitingForSecondPlayer()
    {
        testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 1);

        var result = CallWhatsupViaController(1);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
    }

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn);

        var result = CallWhatsupViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.OpponentsTurn));
        AssertSimpleFleet(result.myFleet, 2, 2);
        AssertSimpleFleet(result.opponentFleet, 1, 1);
        Assert.That(result.gameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        testingEnvironment.CreateNewTestableGame(GameState.HostTurn);

        Assert.That(CallWhatsupViaController(1).gameState, 
            Is.EqualTo(GameStateModel.YourTurn));
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
        var result = controller.WhatsUp(new WhatsupRequestModel { userId =1 });

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
        var gameId = gamePool.Games.Keys.AssertSingle();
        Assert.That(result.gameId, Is.EqualTo(gameId));
    }

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) => 
        new() { userId = userIdParam };

    private static void AssertSimpleFleet(ShipStateModel[]? fleet, int x, int y)
    {
        Assert.That(fleet, Is.Not.Null);
        var ship1 = fleet.AssertSingle();
        var deck1 = ship1.decks.AssertSingle();
        Assert.That(deck1.destroyed, Is.False);
        Assert.That(deck1.x, Is.EqualTo(x));
        Assert.That(deck1.y, Is.EqualTo(y));
    }

    private WhatsUpResponseModel CallWhatsupViaController(int userId) => 
        controller.WhatsUp(CreateWhatsUpRequestModel(userId));
}
