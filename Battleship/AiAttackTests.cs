using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests;

public class AiAttackTests
{
    private TestableGame? game;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    
    public AiAttackTests()
    {
        var services = TestServiceCollection.Minimal();
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
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(1, 1));

        game.Attack(1, new Cell(5, 5));

        Assert.That(game.TimerSecondsLeft, Is.EqualTo(30));
        Assert.That(game.Host.Fleet.AssertSingle().IsDestroyed);
        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
    }

    [Test]
    public void AiTurnRenewsTimer()
    {
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
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
        game = testingEnvironment.CreateNewTestableGame(GameState.HostTurn, 1, 2);
        SetAi(game);
        game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(2, 2) }, 2);
        game.EnqueueAiAttackLocation(new Cell(6, 6));

        game.Attack(1, new Cell(5, 5));

        var locationExcludedByBot = game.Guest!.ExcludedLocations.AssertSingle();
        Assert.That(locationExcludedByBot, Is.EqualTo(new Cell(6, 6)));
        Assert.That(game.State, Is.EqualTo(GameState.HostTurn));
    }
    
    private static void SetAi(Game game) => game.Guest = new User { IsBot = true };
}