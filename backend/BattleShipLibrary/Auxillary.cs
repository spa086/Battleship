using System.Diagnostics.CodeAnalysis;

namespace BattleshipLibrary;

public enum GameState
{
    WaitingForPlayer2,
    BothPlayersCreateFleets,
    WaitingForPlayer2ToCreateFleet,
    Player1Turn,
    Player2Turn
}

public enum AttackResult
{
    Hit,
    Killed,
    Missed,
    Win
}

//todo use DI instead
public static class GamePool
{
    //for testing
    public static void SetGame(Game? game) => TheGame = game;

    public static bool StartPlaying(int userId)
    {
        var result = TheGame is not null;
        if (TheGame is null) TheGame = new Game(userId);
        else TheGame.Start(userId);
        return result;
    }

    //todo does it need to be public?
    public static Game? TheGame { get; private set; }
}

public readonly struct Cell
{
    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public readonly int x;
    public readonly int y;

    public override string ToString() => $"{x},{y}";

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        var otherLocation = (Cell)obj!;
        return otherLocation.x == x && otherLocation.y == y;
    }

    public override int GetHashCode() => x * 100 + y;

    public static bool operator ==(Cell cell1, Cell cell2) => cell1.Equals(cell2);

    public static bool operator !=(Cell cell1, Cell cell2) => cell1.Equals(cell2);
}

public class Deck
{
    //todo tdd this
    public Deck(int x, int y, bool destroyed = false)
    {
        Destroyed = destroyed;
        Location = new Cell(x, y);
    }

    public bool Destroyed { get; set; }

    public Cell Location { get; set; }
}

public class Ship
{
    //todo make it a hashset
    public Dictionary<Cell, Deck> Decks { get; set; } = new Dictionary<Cell, Deck>();
}