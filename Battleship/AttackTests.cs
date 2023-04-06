using NUnit.Framework;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;

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
    private TestableGame game = new(1);
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public AttackTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();

        var serviceProvider = services.BuildServiceProvider();

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
    public void ShotOutsideTheField()
    {
        var exception = Assert.Throws<Exception>(() => game.Attack(1, new Cell(11, 0)));

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
        game = testingEnvironment.CreateNewTestableGame(GameState.GuestTurn);
        game.SetupSimpleFleets(
            new[] { new Cell(0, 1), new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 2);

        game.Attack(1, new Cell(0, 1));

        Assert.That(game.Host!.Fleet!.AssertSingle().Decks[new Cell(0, 1)].Destroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestTurn));
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
        game.SetState(GameState.GuestTurn);

        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
        //todo tdd player ship desctruction
        //todo check 3 times
        game.Host!.Fleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle(); 
    }

    [Test]
    public void Miss()
    {
        game.Attack(0, new Cell(0, 0));

        Assert.That(game.State, Is.EqualTo(GameState.GuestTurn));
        game.Guest!.Fleet!.Where(x => x.Decks.All(x => !x.Value.Destroyed)).AssertSingle();
    }

    //todo does exclusion actually work?

    [Test]
    public void Excluding()
    {
        game.Attack(0, new Cell(1, 1));

        Assert.That(game.Host!.ExcludedLocations.AssertSingle(), Is.EqualTo(new Cell(1, 1)));
    }

    [Test]
    public void AttackAndWin()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 2);

        game.Attack(1, new Cell(2, 2));

        game.Host!.ExcludedLocations.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Guest!.Fleet.AssertSingle()));
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetupSimpleFleets( new[] { new Cell(0, 0)}, 1, new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 0));

        game.Guest!.Fleet.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Host!.Fleet.AssertSingle()));
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }
}