using NUnit.Framework;
using BattleshipLibrary;

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
        GamePool.SetGame(null);
        game.StandardSetup();
    }

    [Test]
    public void Player2CreatesShips()
    {
        game.SetupSimpleFleets(new[] { new Cell(1,1) }, 1, null, null);

        game.CreateAndSaveShips(0, new[] 
        { 
            new Ship 
            {
                Decks = new [] 
                { 
                    new Deck(2,2)
                }.ToDictionary(x => x.Location)
            } 
        });

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
        var deck = game.SecondFleet.AssertSingle().Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(2,2)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(2,2)));
    }

    [Test]
    public void SecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0));

        Assert.That(GamePool.StartPlaying(0), Is.True);
        Assert.That(GamePool.TheGame!.State, Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void StartingAGame()
    {
        GamePool.SetGame(null);

        Assert.That(GamePool.StartPlaying(0), Is.False);

        var game = GamePool.TheGame;
        Assert.That(game, Is.Not.Null);
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForPlayer2));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game.SetupSimpleFleets(null, null, null, null);
        game.CreateAndSaveShips(1, new[]
        {
                new Ship
                {
                    Decks = new[]
                    {
                        new Deck(1, 1), new Deck(1, 2)
                    }.ToDictionary(x => x.Location)
                 }
        });

        //todo use separate collection
        var ship = game.FirstFleet!.AssertSingle();
        var decks = ship.Decks;
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location.y);
        var deck1 = orderedDecks.First();
        Assert.That(deck1.Destroyed, Is.False);
        Assert.That(deck1.Location, Is.EqualTo(new Cell(1, 1)));
        var deck2 = orderedDecks.Last();
        Assert.That(deck2.Destroyed, Is.False);
        Assert.That(deck2.Location, Is.EqualTo(new Cell(1, 2)));
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForPlayer2ToCreateFleet));
    }
}