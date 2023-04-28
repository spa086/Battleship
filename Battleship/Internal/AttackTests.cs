using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Internal;

//todo refactor long file
public class AttackTests
{
    //todo tdd throw if any location list is uninitialized
    private TestableGame? game;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public AttackTests()
    {
        var serviceProvider = 
            TestServiceCollection.Minimal().BuildServiceProvider();

        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
    }

    [SetUp]
    public void SetUp()
    {
        gamePool.ClearGames();
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
    }
    
    [Test]
    public void AttackingInOpponentsTurn()
    {
        testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);

        var exception = Assert.Throws<Exception>(() => game!.Attack(2, new Cell(5, 6)));

        Assert.That(exception.Message, Is.EqualTo("Not your turn."));
    }

    [Test]
    public void AttackingInWrongState([Values] GameState state)
    {
        if (state is GameState.GuestTurn or GameState.HostTurn) return;
        game = testingEnvironment.CreateNewTestableGame(state, 3, 9);

        var exception = Assert.Throws<Exception>(() => game.Attack(3, new Cell(2, 2)))!;
        
        Assert.That(exception.Message, Is.EqualTo($"State not suitable for attack: [{state}]."));
    }

    [Test]
    public void StoppingTimerWhenLost()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2) }, 2);
        game.SetupBattleTimer(100);

        game.Attack(1, new Cell(2, 2));

        Assert.That(game.TimerSecondsLeft, Is.Null);
        Assert.That(game.GetTimer(), Is.Null);
    }

    [Test]
    public void LosingWhenTimeIsOut()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupTurnTime = 1;

        game.Attack(1, new Cell(1, 1));

        TestingEnvironment.SleepMinimalTime();
        Assert.That(game.ItsOver, Is.True);
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
        Assert.That(game.TimerSecondsLeft, Is.LessThanOrEqualTo(0));
    }

    [Test]
    public void ShotOutsideTheField()
    {
        var exception = Assert.Throws<Exception>(() => game!.Attack(1, new Cell(11, 0)))!;

        Assert.That(exception.Message,
            Is.EqualTo("Target cannot be outside the game field. Available coordinates are 0-9."));
    }

    [Test]
    public void TimerRenewal()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.GuestTurn, 1, 2);
        game.SetupBattleTimer(5);

        game.Attack(2, new Cell(0, 0));

        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
    }

    [Test]
    public void DamagingAMultideckShip()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.GuestTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(0, 1), new Cell(0, 0) }, 1,
            new[] { new Cell(2, 2) }, 2);

        var result = game.Attack(2, new Cell(0, 1));

        Assert.That(game.Host.Fleet!.AssertSingle().Decks[new Cell(0, 1)].Destroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestTurn));
        Assert.That(result, Is.EqualTo(AttackResult.Hit));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackSamePlaceTwice()
    {
        game!.SetupExcludedLocations(1, new Cell(0, 0));

        var exception = Assert.Throws<Exception>(() => game.Attack(0, new Cell(0, 0)))!;
        Assert.That(exception.Message, Is.EqualTo("Location [0,0] is already excluded."));
    }

    [Test]
    public void SecondPlayerMisses()
    {
        game!.SetState(GameState.GuestTurn);

        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
        //todo tdd player ship destruction
        //todo check 3 times
        game.Host.Fleet!.Where(ship => ship.Decks.All(deck => !deck.Value.Destroyed)).AssertSingle();
    }

    [Test]
    public void Miss()
    {
        var result = game!.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.GuestTurn));
        game.Guest!.Fleet!
            .Where(ship => ship.Decks.All(deck => !deck.Value.Destroyed)).AssertSingle();
        Assert.That(result, Is.EqualTo(AttackResult.Missed));
    }

    //todo does exclusion actually work?

    [Test]
    public void Excluding()
    {
        game!.Attack(0, new Cell(1, 1));

        Assert.That(game.Host.ExcludedLocations.AssertSingle(), Is.EqualTo(new Cell(1, 1)));
    }

    [Test]
    public void AttackAndWin()
    {
        game!.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1,
            new[] { new Cell(2, 2) }, 2);

        var result = game.Attack(1, new Cell(2, 2));

        game.Host.ExcludedLocations.AssertSingle();
        Assert.That(game.Guest!.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
        Assert.That(result, Is.EqualTo(AttackResult.Win));
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game!.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1,
            new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 0));

        game.Guest!.Fleet.AssertSingle();
        Assert.That(game.Host.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }
}