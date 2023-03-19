using NUnit.Framework;
using BattleshipLibrary;
using System.Numerics;

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
    //todo tdd field borders (and what if nowhere left to fire?)
    //todo tdd 2nd dimension
    private readonly TestableGame game = new(0);

    [SetUp]
    public void SetUp()
    {
        GamePool.ClearGames();
        game.StandardSetup();
    }

    [Test]
    public void FirstPlayerCreatesShipAfterSecondPlayer()
    {
        var game = 
            TestingEnvironment.CreateNewTestableGame(GameState.OnePlayerCreatesFleet, 1, 2, false);
        game.SetupSimpleFleets(null, 1, new[] { new Cell(2, 2) }, 2);

        game.CreateAndSaveShips(1, CreateSimpleShip(1,1));

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void SecondPlayerCreatesShipsWhenFirstHasNoShips()
    {
        var game = 
            TestingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2, false);

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

    private static Ship[] CreateSimpleShip(int x, int y)
    {
        return new[] { new Ship { Decks = new[] { new Deck(x, y) }.ToDictionary(x => x.Location) } };
    }

    [Test]
    public void SecondPlayerJoins()
    {
        GamePool.SetGame(new Game(1));

        Assert.That(GamePool.StartPlaying(1), Is.True);

        Assert.That(GamePool.Games.Values.Single().State, 
            Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void StartingAGame()
    {
        Assert.That(GamePool.StartPlaying(1), Is.False);

        var game = GamePool.Games.Values.AssertSingle();
        Assert.That(game, Is.Not.Null);
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForPlayer2));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game.SetupSimpleFleets(null, null, null, null);
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

    private void AssertNonDestroyedDeck(Deck deck, int x, int y)
    {
        Assert.That(deck.Destroyed, Is.False);
        Assert.That(deck.Location, Is.EqualTo(new Cell(x, y)));
    }
}