using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Internal;

public class GamePoolTests
{
    private TestableGame game = new(0);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    private readonly TestAi testAi;
    private readonly TestMatchingTime testMatchingTime;

    public GamePoolTests()
    {
        var serviceProvider = TestServiceCollection.Minimal().BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        testAi = (serviceProvider.GetService<IAi>() as TestAi)!;
        testMatchingTime = (serviceProvider.GetService<IMatchingTime>() as TestMatchingTime)!;
    }

    [SetUp]
    public void SetUp()
    {
        gamePool.ClearGames();
        game.StandardSetup();
    }

    [TestCase(GameState.GuestWon)]
    [TestCase(GameState.HostWon)]
    [TestCase(GameState.Cancelled)]
    public void CantJoinFinishedGame(GameState state)
    {
        var game = testingEnvironment.CreateNewTestableGame(state, 4);

        Assert.False(gamePool.StartPlaying(4));
    }
    
    [TestCase(GameState.HostTurn)]
    [TestCase(GameState.GuestTurn)]
    [TestCase(GameState.WaitingForGuest)]
    [TestCase(GameState.BothPlayersCreateFleets)]
    [TestCase(GameState.OnePlayerCreatesFleet)]
    public void StartPlayingWhenThereIsAlreadyGame(GameState state)
    {
        var existingGame = testingEnvironment.CreateNewTestableGame(state, 4, 7);

        var exception = Assert.Throws<Exception>(() => gamePool.StartPlaying(4))!;
        Assert.That(exception.Message,
            Is.EqualTo($"Can't start playing: you already participate in ongoing game id=[{existingGame.Id}]."));
    }

    [Test]
    public void GettingGameWhenThereIsNoGame()
    {
        var result = gamePool.GetGame(4799);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GettingGameWhenThereIsAFinishedGame()
    {
        testingEnvironment.CreateNewTestableGame(GameState.GuestTurn, 3, 5);
        testingEnvironment.CreateNewTestableGame(GameState.HostWon, 3, 5);

        var result = gamePool.GetGame(3);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void MatchingTimerCreatesBot()
    {
        testMatchingTime.SetupSeconds = 1;
        testAi.SetupAiShips = CreateSimpleShip(1, 1);

        gamePool.StartPlaying(1);

        TestingEnvironment.SleepMinimalTime();
        var theGame = gamePool.GetGames().Single();
        var botUser = theGame.Guest!;
        Assert.That(botUser, Is.Not.Null);
        Assert.That(botUser.IsBot);
        Assert.That(botUser.Name, Is.EqualTo("General Chaos"));
        var ship = botUser.Fleet.AssertSingle();
        var deck = ship.Decks.Values.AssertSingle();
        Assert.That(deck.Destroyed, Is.False);
        Assert.That(deck.Location, Is.EqualTo(new Cell(1, 1)));
    }

    [Test]
    public void MatchingTimer()
    {
        testMatchingTime.SetupSeconds = 1;
        testAi.SetupAiShips = CreateSimpleShip(1, 1);

        gamePool.StartPlaying(1);

        TestingEnvironment.SleepMinimalTime();
        var theGame = gamePool.GetGames().Single();
        Assert.That(theGame.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
        Assert.That(theGame.TimerSecondsLeft, Is.EqualTo(60));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void GettingGameWhenDoubled(int userIdToSearchBy)
    {
        var game1 = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        var game2 = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var exception = Assert.Throws<Exception>(() => gamePool.GetGame(userIdToSearchBy))!;
        Assert.That(exception.Message, Is.EqualTo($"User id = [{userIdToSearchBy}] participates " +
                                                  $"in several games. Game id's: [{game1.Id}, {game2.Id}]."));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void GettingGameSimple(int userIdToSearchBy)
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var result = gamePool.GetGame(userIdToSearchBy);
        Assert.That(result, Is.EqualTo(game));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 19);
        game.SetupTurnTime = 1;
        gamePool.AddGame(game);

        Assert.That(gamePool.StartPlaying(5), Is.True);

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(1));
        TestingEnvironment.SleepMinimalTime();
        Assert.That(game.State, Is.EqualTo(GameState.Cancelled));
    }

    [Test]
    public void StartingAGame()
    {
        testAi.SetupAiShips = Array.Empty<Ship>();

        Assert.That(gamePool.StartPlaying(1), Is.False);

        var theGame = gamePool.GetGames().AssertSingle();
        Assert.That(theGame, Is.Not.Null);
        Assert.That(theGame.State, Is.EqualTo(GameState.WaitingForGuest));
    }
    
    private static Ship[] CreateSimpleShip(int x, int y) =>
        new[] { new Ship { Decks = new[] { new Deck(x, y) }.ToDictionary(deck => deck.Location) } };
}