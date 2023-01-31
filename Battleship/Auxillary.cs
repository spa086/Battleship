using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public static class TestingEnvironment
{
    public static TestableGame CreateNewTestableGame(
        GameState state = GameState.WaitingForPlayer2,
        int? firstUserId = null, int? secondUserId = null)
    {
        var game = new TestableGame(firstUserId ?? 1).SetState(state);
        GamePool.SetGame(game);
        var testableGame = (GamePool.Games[game.Id] as TestableGame)!;
        var battleOngoing = state == GameState.Player1Turn || state == GameState.Player2Turn;
        var creatingFleets = state == GameState.WaitingForPlayer2ToCreateFleet ||
            state == GameState.BothPlayersCreateFleets;
        if (creatingFleets || battleOngoing)
        {
            testableGame.SetSecondUserId(secondUserId);
            if (battleOngoing)
                testableGame.SetupSimpleFleets(SimpleCellArray(1), 1, SimpleCellArray(3), 2);
            else if (creatingFleets) testableGame.SetupSimpleFleets(SimpleCellArray(1), 1, null, 2);
        }
        return testableGame;
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

