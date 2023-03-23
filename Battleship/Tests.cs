using NUnit.Framework;
using BattleshipLibrary;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;

namespace BattleshipTests;

//lines 105 chars
//methods 20 lines
//files 200 lines
//no partial
//folder 5 files

public class Tests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if ships are adjacent
    //todo tdd game cycle
    private TestableGame game = new(0);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public Tests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();

        var serviceProvider = services.BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>();
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>();
    } 

    [SetUp]
    public void SetUp()
    {
        gamePool.ClearGames();
        game.StandardSetup();
    }

    [TestCase(1)]
    [TestCase(2)]
    public void GettingGameWhenDoubled(int userIdToSearchBy)
    {
        var game1 = testingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);
        var game2 = testingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);

        var exception = Assert.Throws<Exception>(() => gamePool.GetGame(userIdToSearchBy));
        Assert.That(exception.Message, Is.EqualTo($"User id = [{userIdToSearchBy}] participates " +
            $"in several games. Game id's: [{game1.Id}, {game2.Id}]."));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void GettingGameSimple(int userIdToSearchBy)
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);

        var result = gamePool.GetGame(userIdToSearchBy);
        Assert.That(result, Is.EqualTo(game));
    }

    [Test]
    public void StoppingTimerWhenLost()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.SetupNewTurn(100);

        game.Attack(1, new Cell(2, 2));

        Assert.That(game.TurnSecondsLeft, Is.Null);
        Assert.That(game.GetTimer(), Is.Null);
    }

    [Test]
    public void LosingWhenTimeIsOut()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);
        game.SetupTurnTime = 1;

        game.Attack(1, new Cell(1, 1));
        Thread.Sleep(1100);

        Assert.That(game.ItsOver, Is.True);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Won));
        Assert.That(game.TurnSecondsLeft, Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void BatleTimer()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);

        game.CreateAndSaveShips(2, CreateSimpleShip(2, 2));

        Thread.Sleep(1000);
        Assert.That(game.TurnSecondsLeft, Is.EqualTo(29));
    }

    [Test]
    public void FirstPlayerCreatesShipAfterSecondPlayer()
    {
        var game = 
            testingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet, 1, 2, false);
        game.SetupSimpleFleets(null, 1, new[] { new Cell(2, 2) }, 2);

        game.CreateAndSaveShips(1, CreateSimpleShip(1,1));

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
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

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
        Assert.That(game.TurnSecondsLeft, Is.EqualTo(30));
        var deck = game.SecondFleet.AssertSingle().Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(2, 2)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(2, 2)));
    }

    //todo move below
    private static Ship[] CreateSimpleShip(int x, int y)
    {
        return new[] { new Ship { Decks = new[] { new Deck(x, y) }.ToDictionary(x => x.Location) } };
    }

    [Test]
    public void SecondPlayerJoins()
    {
        gamePool.SetGame(new Game(1));

        Assert.That(gamePool.StartPlaying(1), Is.True);

        Assert.That(gamePool.Games.Values.Single().State, 
            Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void StartingAGame()
    {
        Assert.That(gamePool.StartPlaying(1), Is.False);

        var game = gamePool.Games.Values.AssertSingle();
        Assert.That(game, Is.Not.Null);
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForPlayer2));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);
        game.SetupSimpleFleets(null, 1, null, 2);
        var decks = new[] { new Deck(1, 1), new Deck(1, 2) }.ToDictionary(x => x.Location);

        game.CreateAndSaveShips(1, new[] { new Ship { Decks = decks } });

        //todo use separate collection
        var ship = game.FirstFleet!.AssertSingle();
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location.y);
        AssertNonDestroyedDeck(orderedDecks.First(), 1, 1);
        AssertNonDestroyedDeck(orderedDecks.Last(), 1, 2);
        Assert.That(game.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
        Assert.That(game.TurnSecondsLeft, Is.Null);
    }

    private static void AssertNonDestroyedDeck(Deck deck, int x, int y)
    {
        Assert.That(deck.Destroyed, Is.False);
        Assert.That(deck.Location, Is.EqualTo(new Cell(x, y)));
    }
}