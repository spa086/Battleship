using BattleshipApi;
using BattleShipLibrary;
using Microsoft.Extensions.Configuration.UserSecrets;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    [SetUp]
    public void SetUp() => GamePool.SetGame(null);

    //todo tdd finishing the game from controller.

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(2));

        Assert.That(result, Is.EqualTo(GameStateModel.OpponentsTurn));
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(1));

        Assert.That(result, Is.EqualTo(GameStateModel.YourTurn));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.WaitingForPlayer2ToCreateFleet);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel
        {
            ships = new[]
            {
                new ShipTransportModel { decks = new[] { new LocationModel{x=5, y=5 } } },
            },
            userId = 0
        });

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
        var testableGame = CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets);

        var result = CreateController().CreateFleet(new FleetCreationRequestModel
        {
            ships = new[] { new ShipTransportModel { decks = new[] { new LocationModel { x=1, y=1 } } } },
            userId = 1
        });

        Assert.That(result, Is.True);
        var ship = testableGame!.FirstFleet.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.WaitingForPlayer2ToCreateFleet));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = CreateAndGetNewTestableGame();

        AssertControllerReturnValue(x => x.WhatsUp(CreateWhatsUpRequestModel(2)), 
            GameStateModel.CreatingFleet);

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.SecondUserId, Is.EqualTo(2));
    }

    [Test]
    public void FirstPlayerStarts()
    {
        var controller = CreateController();

        var result = controller.WhatsUp(new WhatsupRequestModel { userId =1 });

        Assert.That(result, Is.EqualTo(GameStateModel.WaitingForStart));
        var game = GamePool.TheGame;
        Assert.That(game, Is.Not.Null);
    }

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(CreateController()), Is.EqualTo(expectedValue));

    private static TestableGame CreateAndGetNewTestableGame(
        GameState state = GameState.WaitingForPlayer2)
    {
        GamePool.SetGame(new TestableGame(1).SetState(state));
        var testableGame = (GamePool.TheGame as TestableGame)!;
        if(state == GameState.Player1Turn || state == GameState.Player2Turn)
        {
            testableGame
                .SetSecondUserId(2)
                .SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
                new[] { new Cell(2, 2) }, 2);
        }
        return testableGame;
    }

    private static Controller CreateController() => new();

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int userIdParam = 0)
    {
        return new WhatsupRequestModel { userId = userIdParam };
    }
}
