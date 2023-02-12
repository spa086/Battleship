using BattleshipApi;
using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    [SetUp]
    public void SetUp() => GamePool.ClearGames();

    //todo tdd finishing the game from controller.

    [Test]
    public void GameAbortion([Values] GameState state)
    {
        TestingEnvironment.CreateNewTestableGame(state, 1, 2);
        var gameId = GamePool.Games.Single().Key;

        CreateController().AbortGame(1);

        Assert.That(GamePool.Games.Keys, Does.Not.Contain(gameId));
    }

    [Test]
    public void TwoDecksOfSameShipAreInTheSameLocation()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1);

        var exception = Assert.Throws<Exception>(() =>
            CreateController().CreateFleet(SingleShipFleetCreationRequest(1,
            new[] { new LocationModel { x = 1, y = 1 }, new LocationModel { x = 1, y = 1 } })))!;

        Assert.That(exception.Message, Is.EqualTo("Two decks are at the same place: [1,1]."));
    }

    [Test]
    public void CannotCreateEmptyDecks()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1);
        var controller = CreateController();
        var request = SingleShipFleetCreationRequest(1, null);

        var exception = Assert.Throws<Exception>(() => controller.CreateFleet(request));

        Assert.That(exception.Message, Is.EqualTo("Empty decks are not allowed."));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = TestingEnvironment.CreateNewTestableGame(
            GameState.OnePlayerCreatesFleet, 1, 2);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel 
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
        var testableGame = TestingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel
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

    private static Controller CreateController() => new();

    private static ShipForCreationModel NewSimpleShipForFleetCreationRequest(int x, int y) =>
        new() { decks = new[] { new LocationModel { x = x, y = y } } };

    private static FleetCreationRequestModel SingleShipFleetCreationRequest(int userId,
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
        LocationModel[]? decks) =>
        new() { userId = userId, ships = new[] { new ShipForCreationModel { decks = decks } } };
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
}
