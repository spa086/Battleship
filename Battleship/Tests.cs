using NUnit.Framework;
using BattleshipLibrary;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;

namespace BattleshipTests;

public class Tests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if ships are adjacent
    //todo tdd game cycle
    private TestableGame game = new(0);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    private readonly TestAi testAi;

    public Tests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddSingleton<IAi, TestAi>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        testAi = (serviceProvider.GetService<IAi>() as TestAi)!;
    }

    [SetUp]
    public void SetUp()
    {
        gamePool.ClearGames();
        game.StandardSetup();
    }

    [Test]
    public void MatchingTimer()
    {
        gamePool.SetupMatchingTimeSeconds = 1;
        testAi.SetupAiShips = CreateSimpleShip(1, 1);

        gamePool.StartPlaying(1);

        testingEnvironment.SleepMinimalTime();
        var game = gamePool.Games.Values.Single();
        Assert.That(game.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
        var botUser = game.Guest;
        Assert.That(botUser, Is.Not.Null);
        Assert.That(botUser.IsBot);
        Assert.That(botUser.Name, Is.EqualTo("General Chaos"));
        var ship = botUser.Fleet.AssertSingle();
        var deck = ship.Decks.Values.AssertSingle();
        Assert.That(deck.Destroyed, Is.False);
        Assert.That(deck.Location, Is.EqualTo(new Cell(1, 1)));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void GettingGameWhenDoubled(int userIdToSearchBy)
    {
        var game1 = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        var game2 = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var exception = Assert.Throws<Exception>(() => gamePool.GetGame(userIdToSearchBy));
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
    public void BattleTimer()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);

        game.CreateAndSaveShips(2, CreateSimpleShip(2, 2));

        testingEnvironment.SleepMinimalTime();
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(29));
    }

    [Test]
    public void FirstPlayerCreatesShipAfterSecondPlayer()
    {
        var game = 
            testingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet, 1, 2, false);
        game.SetupSimpleFleets(null, 1, new[] { new Cell(2, 2) }, 2);

        game.CreateAndSaveShips(1, CreateSimpleShip(1,1));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void SecondPlayerCreatesShipsWhenFirstHasNoShips()
    {
        var game = 
            testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2, false);

        game.CreateAndSaveShips(2, CreateSimpleShip(2, 2));


        Assert.That(game.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
    }

    [Test]
    public void Player2CreatesShips()
    {
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, null, 2);

        game.CreateAndSaveShips(2, CreateSimpleShip(2,2));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
        var deck = game.Guest!.Fleet.AssertSingle().Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(2, 2)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(2, 2)));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 1, null);
        game.SetupTurnTime = 1;
        gamePool.AddGame(game);

        Assert.That(gamePool.StartPlaying(1), Is.True);

        Assert.That(game.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(1));
        testingEnvironment.SleepMinimalTime();
        Assert.That(game.State, Is.EqualTo(GameState.Cancelled));
    }

    [Test]
    public void StartingAGame()
    {
        testAi.SetupAiShips = Array.Empty<Ship>();

        Assert.That(gamePool.StartPlaying(1), Is.False);

        var game = gamePool.Games.Values.AssertSingle();
        Assert.That(game, Is.Not.Null);
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForGuest));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);
        game.SetupSimpleFleets(null, 1, null, 2);
        var decks = new[] { new Deck(1, 1), new Deck(1, 2) }.ToDictionary(x => x.Location);

        game.CreateAndSaveShips(1, new[] { new Ship { Decks = decks } });

        //todo use separate collection
        var ship = game.Host!.Fleet!.AssertSingle();
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location.y);
        AssertNonDestroyedDeck(orderedDecks.First(), 1, 1);
        AssertNonDestroyedDeck(orderedDecks.Last(), 1, 2);
        Assert.That(game.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(60));
    }

    private static void AssertNonDestroyedDeck(Deck deck, int x, int y)
    {
        Assert.That(deck.Destroyed, Is.False);
        Assert.That(deck.Location, Is.EqualTo(new Cell(x, y)));
    }

    private static Ship[] CreateSimpleShip(int x, int y) => 
        new[] { new Ship { Decks = new[] { new Deck(x, y) }.ToDictionary(x => x.Location) } };
}