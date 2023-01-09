using Battleship;
using BattleshipApi;
using BattleShipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    [SetUp]
    public void SetUp() => GamePool.ClearGames();

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Started);

        var result = new Controller().WhatsUp(
            new WhatsupRequestModel { SessionId = 0, IsFirstPlayer = false });

        Assert.That(result, Is.EqualTo(WhatsUpResponse.OpponentsTurn));
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Started);

        var result = new Controller().WhatsUp(
            new WhatsupRequestModel { SessionId = 0, IsFirstPlayer = true });

        Assert.That(result, Is.EqualTo(WhatsUpResponse.YourTurn));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.WaitingForSecondPlayerToCreateFleet);

        var result = new Controller().CreateFleet(new FleetCreationRequestModel
        {
            Ships = new[] { new ShipTransportModel { Decks = new[] { 5 } } },
            SessionId = 0
        });

        Assert.That(result, Is.False);
        var ship = testableGame!.Player2Ships.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var pair = ship.Decks.AssertSingle();
        Assert.That(pair.Key, Is.EqualTo(5));
        Assert.That(pair.Value, Is.Not.Null);
        Assert.That(pair.Value.Location, Is.EqualTo(5));
        Assert.That(pair.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.Started));
    }

    [Test]
    public void FirstPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets);

        var result = new Controller().CreateFleet(new FleetCreationRequestModel
        {
            Ships = new[] { new ShipTransportModel { Decks = new[] { 1 } } },
            SessionId = 0
        });

        Assert.That(result, Is.True);
        var ship = testableGame!.Player1Ships.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(1));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(1));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.WaitingForSecondPlayerToCreateFleet));
    }

    [Test]
    public void UserDoesNotJoinButCreatesNewGame()
    {
        CreateAndGetNewTestableGame();

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 1 }),
            WhatsUpResponse.WaitingForStart);
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = CreateAndGetNewTestableGame();

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }), 
            WhatsUpResponse.CreatingFleet);

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void FirstPlayerStarts() =>
        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }),
            WhatsUpResponse.WaitingForStart);

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(new Controller()), Is.EqualTo(expectedValue));

    private static TestableGame CreateAndGetNewTestableGame(
        GameState state = GameState.WaitingForSecondPlayer)
    {
        GamePool.SetGame(new TestableGame(0).SetState(state), 0);
        var testableGame = (GamePool.Games[0] as TestableGame)!;
        if(state == GameState.Started)
        {
            testableGame.SetupSimpleFleets(new[] { 1 }, new[] { 2 });
        }
        return testableGame;
    }
}
