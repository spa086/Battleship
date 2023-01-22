using NUnit.Framework;
using BattleShipLibrary;
using NUnit.Framework.Constraints;

namespace BattleshipTests;

//lines 105 chars
//methods 20 lines
//files 200 lines
//no partial
//project 20 files

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
    public void TwoShipsInTheSameLocation()
    {
        game.SetupSimpleFleets(new[] { new Cell(1, 1), new Cell(1, 2) }, 1, null, null);

        var exception = Assert.Throws<Exception>(() => game.CreateAndSaveShips(0,
            new[] 
            {
                new Ship 
                { 
                    Decks = new[] { new Deck(1, 0), new Deck(1,1) }.ToDictionary(x => x.Location)
                } 
            }));

        Assert.That(exception.Message, Is.EqualTo("Two ships at the same location."));
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

    [Test]
    public void DamagingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 1), new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 1));

        Assert.That(game.FirstFleet!.AssertSingle().Decks[new Cell(0, 1)].Destroyed);
    }

    //todo tdd this but for 1st player turn
    [Test]
    public void DestroyingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 1), new Cell(0, 0) }, 1, new[] { new Cell(2, 2)}, 2);
        game.FirstFleet!.Single().Decks[new Cell(0,0)].Destroyed = true;
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 1));

        var destroyedShip = game.FirstFleet.AssertSingle();
        Assert.That(destroyedShip.Decks.Values.All(x => x.Destroyed));
        Assert.That(Game.IsDestroyed(destroyedShip));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackSamePlaceTwice()
    {
        game.SetupExcludedLocations(new Cell(0, 0));

        var exception = Assert.Throws<Exception>(() => game.Attack(0, new Cell(0,0)));
        Assert.That(exception.Message,
            Is.EqualTo("Location [0,0] is already excluded."));
    }

    [Test]
    public void SecondPlayerMisses()
    {
        game.SetState(GameState.Player2Turn);

        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
        Assert.That(game.Win, Is.False);
        //todo tdd player ship desctruction
        //todo check 3 times
        game.FirstFleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle(); 
    }

    [Test]
    public void Miss()
    {
        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
        Assert.That(game.Win, Is.False);
        game.SecondFleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle();
    }

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        game.Attack(0, new Cell(144, 144));

        Assert.That(game.ExcludedLocations1.AssertSingle(), Is.EqualTo(new Cell(144, 144)));
    }

    [Test]
    public void AttackAndWin()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 2);

        game.Attack(0, new Cell(2, 2));

        game.ExcludedLocations1.AssertSingle();
        Assert.That(Game.IsDestroyed(game.SecondFleet.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetupSimpleFleets( new[] { new Cell(0, 0)}, 1, new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 0));

        game.SecondFleet.AssertSingle();
        Assert.That(Game.IsDestroyed(game.FirstFleet.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
    }
}