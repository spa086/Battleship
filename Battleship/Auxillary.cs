﻿using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class TestAi : IAi
{
    public Ship[]? SetupAiShips { get; set; }
    public Queue<Cell> AttackLocationsQueue { get; set; } = new Queue<Cell>();

    public Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, 
        IEnumerable<Cell> excludedLocations) => 
        AttackLocationsQueue.Dequeue();

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

    public static void SleepMinimalTime() => Thread.Sleep(1100);

    public TestableGame CreateNewTestableGame(GameState state = GameState.WaitingForGuest,
        int? firstUserId = null, int? secondUserId = null, bool hostHasFleet = true, 
        int matchingSeconds = 30)
    {
        var game = new TestableGame(firstUserId ?? 1, matchingSeconds).SetState(state);
        gamePool.AddGame(game);
        MutateGame(game, state, secondUserId, hostHasFleet);
        return game;
    }

    private static void MutateGame(
        TestableGame game, GameState state, int? secondUserId, bool hostHasFleet)
    {
        if (game.State == GameState.WaitingForGuest) game.CreateGuest(secondUserId);
        else if (game.BattleOngoing)
        {
            game.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(2), 2);
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