using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Internal;

public class Tests
{
    //todo tdd throw if ships are adjacent
    private TestableGame game = new(0);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public Tests()
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
    public void FleetGenerationSimple()
    {
        var ai = new Ai();

        for (int i = 0; i < 100; i++)
        {
            var result = ai.GenerateShips();

            Assert.That(result.Length, Is.EqualTo(6));
            var fourDecker = result.Where(x => x.Decks.Values.Count == 4).AssertSingle();
            TestShip(fourDecker, 4);
            var tripleDeckers = result.Where(x => x.Decks.Values.Count == 3).ToArray();
            Assert.That(tripleDeckers.Length, Is.EqualTo(2));
            TestShips(tripleDeckers, 3);
            var doubleDeckers = result.Where(x => x.Decks.Values.Count == 2).ToArray();
            Assert.That(doubleDeckers.Length, Is.EqualTo(3));
            TestShips(doubleDeckers, 2);
        }
    }

    [Test]
    public void NoChoiceForAttackBecauseAllDestroyed()
    {
        var ai = new Ai();
        var ships = (from x in Enumerable.Range(0, 10)
                join y in Enumerable.Range(0, 10) on true equals true
                select new Ship { Decks = new[] { new Deck(x, y, true) }.ToDictionary(deck => deck.Location) })
            .ToList();
        ships.RemoveAll(ship => ship.Decks.Values.Single().Location.X == 5 &&
                                ship.Decks.Values.Single().Location.Y == 6);
        for (int i = 0; i < 100; i++)
        {
            var result = ai.ChooseAttackLocation(ships.ToArray(), Array.Empty<Cell>());

            Assert.That(result.X, Is.EqualTo(5));
            Assert.That(result.Y, Is.EqualTo(6));
        }
    }

    [Test]
    public void NoChoiceForAttackBecauseAllExcluded()
    {
        var ai = new Ai();
        var excludedLocations = (from x in Enumerable.Range(0, 10)
            join y in Enumerable.Range(0, 10) on true equals true
            select new Cell(x, y)).ToList();
        excludedLocations.Remove(new Cell(8, 3));
        for (int i = 0; i < 100; i++)
        {
            var result = ai.ChooseAttackLocation(
                new[] { new Ship { Decks = new[] { new Deck(7, 4) }.ToDictionary(x => x.Location) } },
                excludedLocations);

            Assert.That(result.X, Is.EqualTo(8));
            Assert.That(result.Y, Is.EqualTo(3));
        }
    }

    [Test]
    public void ChooseAttackLocationSimple()
    {
        var ai = new Ai();
        for (int i = 0; i < 100; i++)
        {
            var result = ai.ChooseAttackLocation(
                new[] { new Ship { Decks = new[] { new Deck(7, 5) }.ToDictionary(x => x.Location) } },
                Array.Empty<Cell>());

            Assert.That(result.X, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(9));
            Assert.That(result.Y, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(9));
        }
    }

    [Test]
    public void ChooseAttackLocationWithoutShipLocations() =>
        testingEnvironment.AssertException(
            () => new Ai().ChooseAttackLocation(Array.Empty<Ship>(), Array.Empty<Cell>()),
            "No ships provided for choosing attack location.");

    [Test]
    public void GameStartSimple()
    {
        var theGame = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 100);

        theGame.Start(300);

        Assert.That(theGame.TimerSecondsLeft, Is.EqualTo(60));
        Assert.That(theGame.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
        Assert.That(theGame.Guest, Is.Not.Null);
        Assert.That(theGame.Guest!.Id, Is.EqualTo(300));
    }

    [Test]
    public void GameIdIsGeneratedOnGameCreation() =>
        Assert.That(new Game(1, new TestAi(), TestingEnvironment.LongTime).Id, Is.Not.Zero);

    [Test]
    public void StartPlayingWhenCancelledGameExists()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.Cancelled, 6, 99);

        Assert.DoesNotThrow(() => gamePool.StartPlaying(6));
    }

    [Test]
    public void TimerDisposal()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 14, 3);
        game.SetupBattleTimer(100);

        game.DisposeOfTimer();

        Assert.That(game.Timer, Is.Null);
    }

    [Test]
    public void TechnicalVictory()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 35, 70);

        game.SetTechnicalWinner(true);

        Assert.That(game.Timer, Is.Null);
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
    }

    [Test]
    public void Cancel()
    {
        game.Cancel();

        Assert.That(game.State, Is.EqualTo(GameState.Cancelled));
    }

    private static void TestShips(Ship[] ships, int size)
    {
        foreach (var ship in ships) TestShip(ship, size);
    }

    private static void TestShip(Ship ship, int size)
    {
        var decks = ship.Decks.Values;
        var xDistinctCount = decks.DistinctBy(x => x.Location.X).Count();
        var yDistinctCount = decks.DistinctBy(x => x.Location.Y).Count();
        Assert.That(xDistinctCount == 1 || yDistinctCount == 1);
        var verticalShip = xDistinctCount == 1;
        var minPoint = verticalShip ? decks.Min(deck => deck.Location.Y) : decks.Min(deck => deck.Location.X);
        var expectedShip = Enumerable.Range(minPoint, size);
        var mutableCoordinates =
            verticalShip ? decks.Select(deck => deck.Location.Y) : decks.Select(deck => deck.Location.X);
        var difference = expectedShip.Except(mutableCoordinates);
        Assert.That(difference, Is.Empty);
    }
}