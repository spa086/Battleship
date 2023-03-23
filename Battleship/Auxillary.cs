using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests;

public class TestingEnvironment
{
    private readonly GamePool gamePool;

    public TestingEnvironment(GamePool gamePool)
    {
        this.gamePool = gamePool;
    }

    public TestableGame CreateNewTestableGame(GameState state = GameState.WaitingForPlayer2,
        int? firstUserId = null, int? secondUserId = null, bool firstPlayerHasFleet = true)
    {
        var game = new TestableGame(firstUserId ?? 1).SetState(state);
        gamePool.SetGame(game);
        if (game.CreatingFleets || game.BattleOngoing || game.ItsOver)
        {
            game.SetSecondUserId(secondUserId);
            if (game.BattleOngoing)
            {
                game.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(2), 2);
                game.SetupNewTurn(30);
            }
            else if (game.CreatingFleets) 
                game.SetupSimpleFleets(firstPlayerHasFleet ? SimpleCellArray(1) : null, 1, 
                    firstPlayerHasFleet ? null : SimpleCellArray(2), 2);
            else if (game.ItsOver) SetupGameOver(state, game);
        }
        return game;
    }

    private static void SetupGameOver(GameState state, TestableGame game)
    {
        game.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(2), 2);
        if (state == GameState.Player1Won) game.DestroyFleet(2);
        else game.DestroyFleet(1);
    }

    private static Cell[] SimpleCellArray(int content)
    {
        return new[] { new Cell(content, content) };
    }
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

