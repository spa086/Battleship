using Battleship;
using BattleshipApi;
using BattleShipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    private readonly TestableGame game = new(0);

    [TearDown]
    public void TearDown() => GamePool.SetGame(null);

    [SetUp]
    public void SetUp() => game.StandardSetup();

    [Test]
    public void CreateFleetByController()
    {
        GamePool.SetGame(new TestableGame(0).SetupStarted());

        new Controller().CreateFleet(new[] { new ShipFrontModel { Decks = new[] { 1 } } });

        var ship = game.Player1Ships.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(1));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(1));
        Assert.That(deck.Value.Destroyed, Is.False);
    }

    [Test]
    public void StartingAGameByController()
    {
        new Controller().WhatsUp(new WhatsupRequestModel { SessionId = 0 });

        Assert.That(GamePool.TheGame, Is.Not.Null);
        Assert.That(GamePool.TheGame.Started, Is.False);
    }

    [Test]
    public void WhatsUpAfterSecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0));

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }), 
            WhatsUpResponse.CreatingFleet);
    }

    [Test]
    public void WhatsUpBeforeSecondPlayerJoins() =>
        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }),
            WhatsUpResponse.WaitingForStart);

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(new Controller()), Is.EqualTo(expectedValue));
}
