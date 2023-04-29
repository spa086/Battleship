using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Web;

public class WhatsUpBattleTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public WhatsUpBattleTests()
    {
        var serviceProvider = TestServiceCollection.Web().BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void UserNameIsReturned()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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

    [TestCase(GameState.HostWon, GameStateModel.YouWon)]
    [TestCase(GameState.GuestWon, GameStateModel.OpponentWon)]
    public void WhatsUpWhenWon(GameState gameState, GameStateModel expectedModel)
    {
        testingEnvironment.CreateNewTestableGame(gameState, 1, 2);

        var result = controller.WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result.gameState, Is.EqualTo(expectedModel));
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
    public void GuestWhatsUpAfterShipsOfBothPlayersAreSaved()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var result = CallWhatsUpViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.OpponentsTurn));
        AssertSimpleFleet(result.myFleet, 2, 2);
        AssertSimpleFleet(result.opponentFleet, 1, 1);
        Assert.That(result.gameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void Player1WhatsUpAfterShipsOfBothPlayersAreSaved()
    {
        testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        Assert.That(CallWhatsUpViaController(1).gameState,
            Is.EqualTo(GameStateModel.YourTurn));
    }

    private static WhatsUpRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) =>
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

    private WhatsUpResponseModel CallWhatsUpViaController(int userId) =>
        controller.WhatsUp(CreateWhatsUpRequestModel(userId));
}