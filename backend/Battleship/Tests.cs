using NUnit.Framework;
using BattleShipLibrary;
using BattleshipApi;

namespace Battleship;

//lines 105 chars
//methods 20 lines
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if two ships in same location
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
    public void SecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0), 0);

        Assert.That(GamePool.StartPlaying(0), Is.True);
        Assert.That(GamePool.Games[0].State, Is.EqualTo(GameState.BothPlayersCreateFleets));
    }

    [Test]
    public void StartingAGame()
    {
        Assert.That(GamePool.StartPlaying(0), Is.False);
        var game = GamePool.Games[0];
        Assert.That(game, Is.Not.Null);
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForSecondPlayer));
    }

    [Test]
    public void CreateShipsSimple()
    {
        game.CreateAndSaveShips(new FleetCreationModel
        { Ships = new[] { new ShipCreationModel { Decks = new[] { 1, 2 } } }, IsForPlayer1 = true });

        //todo use separate collection
        var ship = game.Player1Ships!.AssertSingle();
        var decks = ship.Decks;
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location);
        var deck1 = orderedDecks.First();
        Assert.That(deck1.Destroyed, Is.False);
        Assert.That(deck1.Location, Is.EqualTo(1));
        var deck2 = orderedDecks.Last();
        Assert.That(deck2.Destroyed, Is.False);
        Assert.That(deck2.Location, Is.EqualTo(2));
        Assert.That(game.State, Is.EqualTo(GameState.WaitingForSecondPlayerToCreateFleet));
    }

    [Test]
    public void DamagingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        game.SetTurn(false);

        game.Attack(1);

        Assert.That(game.Player1Ships!.AssertSingle().Decks[1].Destroyed);
    }

    //todo tdd this but for 1st player turn
    [Test]
    public void DestroyingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        game.Player1Ships!.Single().Decks[1].Destroyed = true;
        game.SetTurn(false);

        game.Attack(0);

        var destroyedShip = game.Player1Ships.AssertSingle();
        Assert.That(destroyedShip.Decks.Values.All(x => x.Destroyed));
        Assert.That(Game.IsDestroyed(destroyedShip));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackSamePlaceTwice()
    {
        game.SetupExcludedLocations(1);

        var exception = Assert.Throws<Exception>(() => game.Attack(1));
        Assert.That(exception.Message,
            Is.EqualTo("Location [1] is already excluded."));
    }

    //todo similar for 2nd player
    [Test]
    public void Miss()
    {
        game.Attack(0);

        Assert.That(game.Player1Turn, Is.False);
        Assert.That(game.Win, Is.False);
        game.Player2Ships.AssertSingle();
    }

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        game.Attack(144);

        //Assert.That(game.ExcludedLocations1.AssertSingle(), Is.EqualTo(144));
    }

    [Test]
    public void AttackAndWin()
    {
        game.Attack(2);

        game.ExcludedLocations1.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Player2Ships.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.Player1Turn);
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetTurn(false);

        game.Attack(1);

        game.Player2Ships.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Player1Ships.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.Player1Turn, Is.False);
    }
}