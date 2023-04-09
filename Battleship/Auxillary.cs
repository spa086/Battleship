using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests;

public class TestAi : IAi
{
    public Ship[]? SetupAiShips { get; set; }
    public Cell? SetupAttackLocation { get; set; }

    public Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, IEnumerable<Cell> excludedLocations) => 
        SetupAttackLocation ?? throw new Exception($"Attack location is not set up.");

    public Ship[] GenerateShips()
    {
        return SetupAiShips ?? throw new Exception($"Ships are not set up.");
    }
}

public class TestingEnvironment
{
    private readonly GamePool gamePool;

    public TestingEnvironment(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public void SleepMinimalTime() => Thread.Sleep(1100);

    public TestableGame CreateNewTestableGame(GameState state = GameState.WaitingForGuest,
        int? firstUserId = null, int? secondUserId = null, bool hostHasFleet = true)
    {
        var game = new TestableGame(firstUserId ?? 1).SetState(state);
        gamePool.AddGame(game);
        if (game.CreatingFleets || game.BattleOngoing || game.ItsOver)
            MutateGame(game, state, secondUserId, hostHasFleet);
        else if (game.State != GameState.WaitingForGuest) throw new Exception("Unknown situation.");
        return game;
    }

    private static void MutateGame(
        TestableGame game, GameState state, int? secondUserId, bool hostHasFleet)
    {
        game.SetSecondUserId(secondUserId);
        if (game.BattleOngoing)
        {
            game.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(2), 2);
            game.SetupBattleTimer(30);
        }
        else if (game.CreatingFleets) SetupGameInCreatingFleets(hostHasFleet, game);
        else if (game.ItsOver) SetupGameOver(state, game);
    }

    private static void SetupGameInCreatingFleets(bool firstPlayerHasFleet, TestableGame game)
    {
        game.SetupSimpleFleets(firstPlayerHasFleet ? SimpleCellArray(1) : null, 1,
            firstPlayerHasFleet ? null : SimpleCellArray(2), 2);
        game.SetupShipsCreationTimer(60);
    }

    private static void SetupGameOver(GameState state, TestableGame game)
    {
        game.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(2), 2);
        if (state == GameState.HostWon) game.DestroyFleet(2);
        else if (state == GameState.GuestWon) game.DestroyFleet(1);
        else if (state == GameState.Cancelled) { }
        else throw new Exception($"Unknown state: [{state}].");
    }

    private static Cell[] SimpleCellArray(int content) => new[] { new Cell(content, content) };
}

public static class Extensions
{
    //todo tdd what if it is null
    public static T AssertSingle<T>(this IEnumerable<T>? collection)
    {
        if (collection is null) throw new Exception("AssertSingle requires non-null target.");
        Assert.That(collection.Count(), Is.EqualTo(1));
        return collection.Single();
    }
}