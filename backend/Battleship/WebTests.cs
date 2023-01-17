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
    public void GameAbortion()
    {
        CreateAndGetNewTestableGame();

        CreateController().AbortGame(new GameAbortionRequestModel { SessionId = 0});

        var count = GamePool.Games.Count;
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(0, false));

        Assert.That(result, Is.EqualTo(WhatsUpResponse.OpponentsTurn));
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(0, true));

        Assert.That(result, Is.EqualTo(WhatsUpResponse.YourTurn));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.WaitingForPlayer2ToCreateFleet);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel
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
        Assert.That(testableGame.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void FirstPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel
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
        Assert.That(testableGame.State, Is.EqualTo(GameState.WaitingForPlayer2ToCreateFleet));
    }

    [Test]
    public void UserDoesNotJoinButCreatesNewGame()
    {
        CreateAndGetNewTestableGame();

        AssertControllerReturnValue(x => x.WhatsUp(CreateWhatsUpRequestModel(1)),
            WhatsUpResponse.WaitingForStart);
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = CreateAndGetNewTestableGame();

        AssertControllerReturnValue(x => x.WhatsUp(CreateWhatsUpRequestModel()), 
            WhatsUpResponse.CreatingFleet);

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void FirstPlayerStarts() =>
        AssertControllerReturnValue(x => x.WhatsUp(CreateWhatsUpRequestModel()),
            WhatsUpResponse.WaitingForStart);

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(CreateController()), Is.EqualTo(expectedValue));

    private static TestableGame CreateAndGetNewTestableGame(
        GameState state = GameState.WaitingForPlayer2)
    {
        GamePool.SetGame(new TestableGame(0).SetState(state), 0);
        var testableGame = (GamePool.Games[0] as TestableGame)!;
        if(state == GameState.Player1Turn)
        {
            testableGame.SetupSimpleFleets(new[] { 1 }, new[] { 2 });
        }
        return testableGame;
    }

    private static Controller CreateController() => new();

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int sessionId = 0, 
        bool? isFirstPlayer = null)
    {
        return new WhatsupRequestModel { SessionId = sessionId, IsFirstPlayer = isFirstPlayer };
    }
}
