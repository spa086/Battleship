using BattleshipApi;
using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WhatsUpTests
{
    [SetUp]
    public void SetUp() => GamePool.ClearGames();

    [TestCase(GameState.WaitingForPlayer2ToCreateFleet, true)]
    [TestCase(GameState.WaitingForPlayer2ToCreateFleet, false)]
    [TestCase(GameState.BothPlayersCreateFleets, true)]
    [TestCase(GameState.BothPlayersCreateFleets, false)]
    public void WhatsUpWhileCreatingShips(GameState state, bool firstPlayer)
    {
        TestingEnvironment.CreateNewTestableGame(state, 1, 2);

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(firstPlayer ? 1 : 2));

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.CreatingFleet));
    }

    [Test]
    public void OpponentExcludedLocations()
    {
        var game = TestingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);
        game.SetupExcludedLocations(1, new Cell(5, 6));

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(2));

        var location = result.opponentExcludedLocations.AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void MyExcludedLocations()
    {
        var game = TestingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);
        game.SetupExcludedLocations(1, new Cell(5, 6));

        var result = CreateController().WhatsUp(CreateWhatsUpRequestModel(1));

        var location = result.myExcludedLocations.AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void FirstPlayerWhatsupWhileWaitingForSecondPlayer()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.WaitingForPlayer2, 1);

        var result = CallWhatsupViaController(1);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.WaitingForStart));
    }

    [Test]
    public void Player2WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.Player1Turn);

        var result = CallWhatsupViaController(2);

        Assert.That(result.gameState, Is.EqualTo(GameStateModel.OpponentsTurn));
        AssertSimpleFleet(result.myFleet, 3, 3);
        AssertSimpleFleet(result.opponentFleet, 1, 1);
    }

    [Test]
    public void Player1WhatsupAfterShipsOfBothPlayersAreSaved()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.Player1Turn);

        Assert.That(CallWhatsupViaController(1).gameState, 
            Is.EqualTo(GameStateModel.YourTurn));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = TestingEnvironment.CreateNewTestableGame(GameState.WaitingForPlayer2, 
            1, 2);

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
        var gameId = GamePool.Games.Keys.AssertSingle();
        Assert.That(result.gameId, Is.EqualTo(gameId));
    }

    private static Controller CreateController() => new();

    private static WhatsupRequestModel CreateWhatsUpRequestModel(int userIdParam = 0) => 
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

    private static WhatsUpResponseModel CallWhatsupViaController(int userId) => 
        CreateController().WhatsUp(CreateWhatsUpRequestModel(userId));
}
