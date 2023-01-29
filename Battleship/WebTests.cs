using BattleshipApi;
using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebTests
{
    [SetUp]
    public void SetUp() => GamePool.SetGame(null);

    //todo tdd finishing the game from controller.

    [Test]
    public void ReturningExcludedLocationsFor([Values] bool firstPlayer)
    {
        var game = CreateAndGetNewTestableGame(GameState.Player1Turn, 1, 2);
        game.SetupExcludedLocations(firstPlayer ? 1 : 2, new Cell(5, 6));

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(firstPlayer ? 1 : 2));

        var location = 
            (firstPlayer ? result.excludedLocations1 : result.excludedLocations2).AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void GameAbortion([Values] GameState state)
    {
        CreateAndGetNewTestableGame(state, 1, 2);

        CreateController().AbortGame(1);

        Assert.That(GamePool.TheGame, Is.Null);
    }

    [Test]
    public void TwoDecksOfSameShipAreInTheSameLocation()
    {
        CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets, 1);

        var exception = Assert.Throws<Exception>(() =>
            CreateController().CreateFleet(SingleShipFleetCreationRequest(1,
            new[] { new LocationModel { x = 1, y = 1 }, new LocationModel { x = 1, y = 1 } })))!;

        Assert.That(exception.Message, Is.EqualTo("Two decks are at the same place: [1,1]."));
    }

    [Test]
    public void CannotCreateEmptyDecks()
    {
        CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets, 1);
        var controller = CreateController();
        var request = SingleShipFleetCreationRequest(1, null);

        var exception = Assert.Throws<Exception>(() => controller.CreateFleet(request));

        Assert.That(exception.Message, Is.EqualTo("Empty decks are not allowed."));
    }

    [Test]
    public void FirstPlayerWhatsupWhileWaitingForSecondPlayer()
    {
        CreateAndGetNewTestableGame(GameState.WaitingForPlayer2, 1);

        var result = CallWhatsupViaController(1);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
    }

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        var result = CallWhatsupViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.OpponentsTurn));
        AssertSimpleFleet(result.fleet1, 1, 1);
        AssertSimpleFleet(result.fleet2, 3, 3);
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        CreateAndGetNewTestableGame(GameState.Player1Turn);

        Assert.That(CallWhatsupViaController(1).gameState, 
            Is.EqualTo(GameStateModel.YourTurn));
    }

    [Test]
    public void SecondPlayerCreatesFleet()
    {
        var testableGame = CreateAndGetNewTestableGame(GameState.WaitingForPlayer2ToCreateFleet);

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
        var testableGame = CreateAndGetNewTestableGame(GameState.BothPlayersCreateFleets);

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
        Assert.That(testableGame.State, Is.EqualTo(GameState.WaitingForPlayer2ToCreateFleet));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = CreateAndGetNewTestableGame();

        var result = CallWhatsupViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.SecondUserId, Is.EqualTo(2));
    }

    [Test]
    public void FirstPlayerStarts()
    {
        var result = CreateController().WhatsUp(new WhatsupRequestModel { userId =1 });

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
        var game = GamePool.TheGame;
        Assert.That(game, Is.Not.Null);
    }

    private static TestableGame CreateAndGetNewTestableGame(
        GameState state = GameState.WaitingForPlayer2, int? firstUserId = null, int? secondUserId = null)
    {
        GamePool.SetGame(new TestableGame(firstUserId ?? 1).SetState(state));
        var testableGame = (GamePool.TheGame as TestableGame)!;
        if(state == GameState.Player1Turn || state == GameState.Player2Turn)
            testableGame.SetSecondUserId(secondUserId)
                .SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(3, 3) }, 2);
        return testableGame;
    }

    private static Controller CreateController() => new();

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) => 
        new() { userId = userIdParam };

    private static ShipForCreationModel NewSimpleShipForFleetCreationRequest(int x, int y) =>
        new() { decks = new[] { new LocationModel { x = x, y = y } } };

    private static void AssertSimpleFleet(ShipStateModel[]? fleet, int x, int y)
    {
        Assert.That(fleet, Is.Not.Null);
        var ship1 = fleet.AssertSingle();
        var deck1 = ship1.decks.AssertSingle();
        Assert.That(deck1.destroyed, Is.False);
        Assert.That(deck1.x, Is.EqualTo(x));
        Assert.That(deck1.y, Is.EqualTo(y));
    }

    private static WhatsUpResponseModel CallWhatsupViaController(int userId) => 
        CreateController().WhatsUp(CreateWhatsUpRequestModel(userId));

    private static FleetCreationRequestModel SingleShipFleetCreationRequest(int userId,
        LocationModel[]? decks) =>
        new() { userId = userId, ships = new[] { new ShipForCreationModel { decks = decks ?? null } } };
}
