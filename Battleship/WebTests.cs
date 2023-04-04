using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    private readonly WebResult webResult;

    public WebTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddTransient<Controller>();
        services.AddTransient<WebResult>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
        webResult = serviceProvider.GetService<WebResult>()!;
    }

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void AbortionWhenVictoryHasAlreadyHappened()
    {
        gamePool.ClearGames();
        var game = testingEnvironment.CreateNewTestableGame(GameState.Player1Won, 1, 2);

        controller.AbortGame(1);

        Assert.That(gamePool.Games, Has.Count.Zero);
    }

    [Test]
    public void SettingSecondUserName()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets);
        var request = SingleShipFleetCreationRequest(2, new[] { new LocationModel { x = 1, y = 1 } });
        request.userName = "Rachel";

        controller.CreateFleet(request);

        Assert.That(game.SecondUserName, Is.EqualTo("Rachel"));
    }

    [Test]
    public void SettingFirstUserName()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets);
        var request = SingleShipFleetCreationRequest(1, new[] {new LocationModel { x = 1, y = 1} });
        request.userName = "Boris";

        controller.CreateFleet(request);

        Assert.That(game.FirstUserName, Is.EqualTo("Boris"));
    }

    [Test]
    public void SimpleGettingRequestModel()
    {
        var result = webResult.GetRequestModel<int>("5");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrepareErrorResult()
    {
        var result = webResult.Prepare<int, int>((m, c) => throw new Exception("Some error."), "5");

        Assert.That(result, Is.EqualTo("\"Some error.\""));
    }

    [Test]
    public void PrepareResult()
    {
        var result = webResult.Prepare<int, int>((m, c) => 2, "5");

        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void Surrendering([Values] GameState state)
    {
        if (state == GameState.Player1Won || state == GameState.Player2Won) return;
        testingEnvironment.CreateNewTestableGame(state, 1, 2);

        controller.AbortGame(1);

        var game = gamePool.Games.Values.Single();
        Assert.That(game.State, Is.EqualTo(GameState.Player2Won));
    }

    [Test]
    public void TwoDecksOfSameShipAreInTheSameLocation()
    {
        testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1);

        var exception = Assert.Throws<Exception>(() =>
            controller.CreateFleet(SingleShipFleetCreationRequest(1,
            new[] { new LocationModel { x = 1, y = 1 }, new LocationModel { x = 1, y = 1 } })))!;

        Assert.That(exception.Message, Is.EqualTo("Two decks are at the same place: [1,1]."));
    }

    [Test]
    public void CannotCreateEmptyDecks()
    {
        testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1);
        var request = SingleShipFleetCreationRequest(1, null);

        var exception = Assert.Throws<Exception>(() => controller.CreateFleet(request));

        Assert.That(exception.Message, Is.EqualTo("Empty decks are not allowed."));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = testingEnvironment.CreateNewTestableGame(
            GameState.OnePlayerCreatesFleet, 1, 2);

        var result = controller.CreateFleet(new FleetCreationRequestModel 
            { ships = new[] { NewSimpleShipForFleetCreationRequest(5, 5) }, userId = 2 });

        Assert.That(result, Is.False);
        var ship = testableGame!.SecondFleet.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var pair = ship.Decks.AssertSingle();
        Assert.That(pair.Key, Is.EqualTo(new Cell(5,5)));
        Assert.That(pair.Value, Is.Not.Null);
        Assert.That(pair.Value.Location, Is.EqualTo(new Cell(5, 5)));
        Assert.That(pair.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void FirstPlayerCreatesFleet()
    {
        var testableGame = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets);

        var result = controller.CreateFleet(new FleetCreationRequestModel
        { ships = new[] { NewSimpleShipForFleetCreationRequest(1, 1) }, userId = 1 });

        Assert.That(result, Is.True);
        var ship = testableGame!.FirstFleet.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
    }

    private static ShipForCreationModel NewSimpleShipForFleetCreationRequest(int x, int y) =>
        new() { decks = new[] { new LocationModel { x = x, y = y } } };

    private static FleetCreationRequestModel SingleShipFleetCreationRequest(int userId,
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
        LocationModel[]? decks) =>
        new() { userId = userId, ships = new[] { new ShipForCreationModel { decks = decks } } };
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
}
