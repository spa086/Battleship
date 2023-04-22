using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Web;

public class WebAttackTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public WebAttackTests()
    {
        //todo 3 times
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddTransient<Controller>();
        services.AddSingleton<IAi, TestAi>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void AttackReturnsUserName()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupUserName(1, "space ranger");

        var result = controller.Attack(new AttackRequestModel { userId = 1 });

        Assert.That(result.opponentName, Is.EqualTo("space ranger"));
    }

    [Test]
    public void AttackingInOpponentsTurn()
    {
        testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var exception = Assert.Throws<Exception>(() => controller.Attack(
            new AttackRequestModel { location = new LocationModel { x = 5, y = 6 }, userId = 2 }))!;

        Assert.That(exception.Message, Is.EqualTo("Not your turn."));
    }

    [Test]
    public void ReturningExcludedLocationsFor([Values] bool firstPlayer)
    {
        testingEnvironment.CreateNewTestableGame(
            firstPlayer ? GameState.HostTurn : GameState.GuestTurn,
            1, 2);

        var result = controller.Attack(
            new AttackRequestModel
            {
                location = new LocationModel { x = 5, y = 6 },
                userId = firstPlayer ? 1 : 2
            });

        var location = (firstPlayer ? result.excludedLocations1 : result.excludedLocations2)
            .AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void AttackMissed()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(3, 3) }, 2);

        var result = controller.Attack(new AttackRequestModel
            { location = new LocationModel { x = 2, y = 2 }, userId = 1 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Missed));
        Assert.That(game.State, Is.EqualTo(GameState.GuestTurn));
    }

    [Test]
    public void AttackHitsAShip()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2), new Cell(2, 3) }, 2);
        var request = new AttackRequestModel { location = new LocationModel { x = 2, y = 2 }, userId = 1 };

        var result = controller.Attack(request);

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Hit));
        var decks = game.Guest!.Fleet.AssertSingle().Decks.Values;
        Assert.That(decks, Has.Count.EqualTo(2));
        Assert.That(decks.Where(x => x.Location == new Cell(2, 2)).AssertSingle().Destroyed, Is.True);
        Assert.That(decks.Where(x => x.Location == new Cell(2, 3)).AssertSingle().Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void Player21AttacksAndWins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.GuestTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2) }, 2);

        var result = controller.Attack(new AttackRequestModel
            { location = new LocationModel { x = 1, y = 1 }, userId = 2 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        AssertSimpleDeckDestroyed(game.Host.Fleet!, true);
        AssertSimpleDeckDestroyed(game.Guest!.Fleet!, false);
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }

    [Test]
    public void Player1AttacksAndWins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2) }, 2);

        var result = controller.Attack(new AttackRequestModel
            { location = new LocationModel { x = 2, y = 2 }, userId = 1 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        AssertSimpleDeckDestroyed(game.Host.Fleet!, false);
        AssertSimpleDeckDestroyed(game.Guest!.Fleet!, true);
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
    }

    private static void AssertSimpleDeckDestroyed(Ship[] ships, bool expectingDestroyed) =>
        Assert.That(ships.Single().Decks.Single().Value.Destroyed,
            Is.EqualTo(expectingDestroyed));
}