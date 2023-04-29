using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Internal;

public class ShipCreationTests
{
    //todo tdd throw if ships are adjacent
    private TestableGame game = new(0);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public ShipCreationTests()
    {
        var serviceProvider = TestServiceCollection.Minimal().BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
    }

    [SetUp]
    public void SetUp()
    {
        gamePool.ClearGames();
        game.StandardSetup();
    }
    
    [Test]
    public void BattleTimer()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);

        game.SaveShips(2, CreateSimpleShip(2, 2));

        TestingEnvironment.SleepMinimalTime();
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(29));
    }

    [Test]
    public void FirstPlayerCreatesShipAfterSecondPlayer()
    {
        game = testingEnvironment.CreateNewTestableGame(
            GameState.OnePlayerCreatesFleet, 1, 2, false);
        game.SetupSimpleFleets(null, new[] { new Cell(2, 2) });

        game.SaveShips(1, CreateSimpleShip(1, 1));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void SecondPlayerCreatesShipsWhenFirstHasNoShips()
    {
        game = testingEnvironment.CreateNewTestableGame(
            GameState.BothPlayersCreateFleets, 1, 2, false);

        game.SaveShips(2, CreateSimpleShip(2, 2));


        Assert.That(game.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
    }

    [Test]
    public void Player2CreatesShips()
    {
        game.SetupSimpleFleets(new[] { new Cell(1, 1) });

        game.SaveShips(2, CreateSimpleShip(2, 2));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
        var deck = game.Guest!.Fleet.AssertSingle().Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(2, 2)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(2, 2)));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);
        game.SetupSimpleFleets(null);
        var decks = new[] { new Deck(1, 1), new Deck(1, 2) }.ToDictionary(x => x.Location);

        game.SaveShips(1, new[] { new Ship { Decks = decks } });

        game.Host.Fleet!.AssertSingle();
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location.Y).ToArray();
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
        new[] { new Ship { Decks = new[] { new Deck(x, y) }.ToDictionary(deck => deck.Location) } };
}