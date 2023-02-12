using NUnit.Framework;
using BattleshipLibrary;

namespace BattleshipTests;

//lines 105 chars
//methods 20 lines
//files 200 lines
//no partial
//folder 5 files

public class AttackTests
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
    public void DamagingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 1), new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 
            2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 1));

        Assert.That(game.FirstFleet!.AssertSingle().Decks[new Cell(0, 1)].Destroyed);
    }

    //todo tdd this but for 1st player turn
    [Test]
    public void DestroyingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 1), new Cell(0, 0) }, 1, new[] { new Cell(2, 2)}, 
            2);
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
        game.SetupExcludedLocations(1, new Cell(0, 0));

        var exception = Assert.Throws<Exception>(() => game.Attack(0, new Cell(0,0)));
        Assert.That(exception.Message, Is.EqualTo("Location [0,0] is already excluded."));
    }

    [Test]
    public void SecondPlayerMisses()
    {
        game.SetState(GameState.Player2Turn);

        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
        //todo tdd player ship desctruction
        //todo check 3 times
        game.FirstFleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle(); 
    }

    [Test]
    public void Miss()
    {
        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
        game.SecondFleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle();
    }

    //todo does exclusion actually work?

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

        game.Attack(1, new Cell(2, 2));

        game.ExcludedLocations1.AssertSingle();
        Assert.That(Game.IsDestroyed(game.SecondFleet.AssertSingle()));
        Assert.That(game.State, Is.EqualTo(GameState.Player1Won));
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetupSimpleFleets( new[] { new Cell(0, 0)}, 1, new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 0));

        game.SecondFleet.AssertSingle();
        Assert.That(Game.IsDestroyed(game.FirstFleet.AssertSingle()));
        Assert.That(game.State, Is.EqualTo(GameState.Player2Won));
    }
}