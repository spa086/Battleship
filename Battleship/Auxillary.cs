using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class TestAi : IAi
{
    public Ship[]? SetupAiShips { get; set; }
    public Queue<Cell> AttackLocationsQueue { get; set; } = new();

    public Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, IEnumerable<Cell> excludedLocations) =>
        AttackLocationsQueue.Dequeue();

    public Ship[] GenerateShips() => SetupAiShips ?? throw new Exception($"Ships are not set up.");
}

public class TestingEnvironment
{
    private readonly GamePool gamePool;

    public TestingEnvironment(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public static void SleepMinimalTime() => Thread.Sleep(1100);

    public TestableGame CreateNewTestableGame(GameState state = GameState.WaitingForGuest, int? hostId = null,
        int? guestId = null, bool hostHasFleet = true, int matchingSeconds = LongTime)
    {
        var game = new TestableGame(hostId ?? 1, matchingSeconds).SetState(state);
        gamePool.AddGame(game);
        MutateGame(game, state, hostId, guestId, hostHasFleet);
        return game;
    }

    private static void MutateGame(
        TestableGame game, GameState state, int? hostId, int? guestId, bool hostHasFleet)
    {
        if (game.State != GameState.WaitingForGuest) game.CreateGuest(guestId);
        if (game.State == GameState.WaitingForGuest)
        {
        }
        else if (game.BattleOngoing)
        {
            game.SetupSimpleFleets(SimpleCellArray(1), hostId,
                SimpleCellArray(2), guestId);
            game.SetupBattleTimer(30);
        }
        else if (game.CreatingFleets) SetupGameInCreatingFleets(hostHasFleet, game);
        else if (game.ItsOver) SetupGameOver(state, game);
        else throw new Exception("Unknown situation.");
    }

    private static void SetupGameInCreatingFleets(bool firstPlayerHasFleet, TestableGame game)
    {
        game.SetupSimpleFleets(firstPlayerHasFleet ? SimpleCellArray(1) : null, 1,
            firstPlayerHasFleet ? null : SimpleCellArray(2), 2);
        game.SetupShipsCreationTimer(60);
    }

    private static void SetupGameOver(GameState state, TestableGame game)
    {
        game.SetupSimpleFleets(SimpleCellArray(1), 1,
            SimpleCellArray(2), 2);
        if (state == GameState.HostWon) game.DestroyFleet(2);
        else if (state == GameState.GuestWon) game.DestroyFleet(1);
        else if (state == GameState.Cancelled)
        {
        }
        else throw new Exception($"Unknown state: [{state}].");
    }

    private static Cell[] SimpleCellArray(int content) => new[] { new Cell(content, content) };

    private const int LongTime = 36000;
}

public static class Extensions
{
    //todo tdd what if it is null
    public static T AssertSingle<T>(this IEnumerable<T>? collection)
    {
        if (collection is null) throw new Exception("AssertSingle requires non-null target.");
        var array = collection.ToArray();
        Assert.That(array.Length, Is.EqualTo(1));
        return array.Single();
    }
}