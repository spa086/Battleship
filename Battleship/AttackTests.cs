using NUnit.Framework;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;

namespace BattleshipTests;

//todo refactor long file
public class AttackTests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if ships are adjacent
    private TestableGame game;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;

    public AttackTests()
    {
        //todo 3 times
        var services = new ServiceCollection();
        services.AddSingleton<GamePool>();
        services.AddTransient<TestingEnvironment>();
        services.AddSingleton<IAi, TestAi>();

        var serviceProvider = services.BuildServiceProvider();

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
    public void AiAttacksAgain()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1), new Cell(1, 2) }, 1,
            new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(1, 1));
        game.EnqueueAiAttackLocation(new Cell(1, 2));

        game.Attack(1, new Cell(5, 5));

        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }

    [Test]
    public void AttackAgainWithAi()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2), new Cell(2, 3) }, 2);
        game.EnqueueAiAttackLocation(new Cell(1, 1));

        game.Attack(1, new Cell(2, 2));

        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void AiWins()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(1, 1));

        game.Attack(1, new Cell(5, 5));

        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
        Assert.That(game.Host!.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }

    [Test]
    public void AiTurnRenewsTimer()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(6, 6));
        game.SetupBattleTimer(100);

        game.Attack(1, new Cell(5, 5));

        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
    }

    [Test]
    public void AiHits()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        var hostDecks = new[] { new Deck(1, 1), new Deck(1, 2) }.ToDictionary(x => x.Location);
        var guestDecks = new[] { new Deck(3, 3) }.ToDictionary(x => x.Location);
        game.SetupFleets(
            new[] { new Ship { Decks = hostDecks } }, new[] { new Ship { Decks = guestDecks } });
        game.EnqueueAiAttackLocation(new Cell(1, 1));
        game.EnqueueAiAttackLocation(new Cell(7, 7));

        game.Attack(1, new Cell(5, 5));

        var ship = game.Host.Fleet.AssertSingle();
        var deck = ship.Decks[new Cell(1, 1)];
        Assert.That(deck.Destroyed, Is.True);
    }

    [Test]
    public void AiMisses()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(6, 6));

        game.Attack(1, new Cell(5, 5));

        var locationExcludedByBot = game.Guest!.ExcludedLocations.AssertSingle();
        Assert.That(locationExcludedByBot, Is.EqualTo(new Cell(6, 6)));
        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void StoppingTimerWhenLost()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.SetupBattleTimer(100);

        game.Attack(1, new Cell(2, 2));

        Assert.That(game.TimerSecondsLeft, Is.Null);
        Assert.That(game.GetTimer(), Is.Null);
    }

    [Test]
    public void LosingWhenTimeIsOut()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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
        game = testingEnvironment.CreateNewTestableGame(GameState.GuestTurn, 1, 2);
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

        var exception = Assert.Throws<Exception>(() => game.Attack(0, new Cell(0, 0)));
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
        Assert.That(game.Guest!.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.HostWon));
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1, new[] { new Cell(2, 2) }, 2);
        game.SetTurn(false);

        game.Attack(0, new Cell(0, 0));

        game.Guest!.Fleet.AssertSingle();
        Assert.That(game.Host!.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }

    private static void SetAi(TestableGame game) => game.Guest = new User { IsBot = true };
}