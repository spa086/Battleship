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
    public void CreateFleetByController()
    {
        GamePool.SetGame(new TestableGame(0).SetupStarted(), 0);

        new Controller().CreateFleet(new FleetCreationRequestModel
        {
            Ships = new[] { new ShipTransportModel { Decks = new[] { 1 } } },
            SessionId = 0
        });

        var testableGame = GamePool.Games[0] as TestableGame;
        var ship = testableGame!.Player1Ships.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(1));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(1));
        Assert.That(deck.Value.Destroyed, Is.False);
    }

    [Test]
    public void UserDoesNotJoinButCreatesNewGame()
    {
        GamePool.SetGame(new Game(0), 0);

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 1 }),
            WhatsUpResponse.WaitingForStart);
    }

    [Test]
    public void SecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0), 0);

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }), 
            WhatsUpResponse.CreatingFleet);
    }

    [Test]
    public void FirstPlayerStarts() =>
        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }),
            WhatsUpResponse.WaitingForStart);

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(new Controller()), Is.EqualTo(expectedValue));
}
