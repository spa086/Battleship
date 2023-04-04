using NLog;
using System.Diagnostics.CodeAnalysis;

namespace BattleshipLibrary;

public static class Log
{
    public static void Error(Exception ex)
    {
        Logger.Error(ex);
        Console.WriteLine(ex);
    }

    public static void Info(string message)
    {
        Logger.Info(message);
        Console.WriteLine(message);
    }

    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
}

public enum GameState
{
    WaitingForPlayer2,
    BothPlayersCreateFleets,
    OnePlayerCreatesFleet,
    Player1Turn,
    Player2Turn,
    Player1Won,
    Player2Won
}

public enum AttackResult
{
    Hit,
    Missed,
    Win
}

public class GamePool
{
    public GamePool()
    {

    }

    public Game? GetGame(int userId)
    {
        var gamesByUserId = Games.Values.Where(x => 
            x.FirstUserId == userId || x.SecondUserId == userId);
        if (gamesByUserId.Count() > 1)
            throw new Exception($"User id = [{userId}] participates in several games. Game id's: " +
                $"[{string.Join(", ", gamesByUserId.Select(x => x.Id))}].");
        return gamesByUserId.SingleOrDefault();
    }

    //todo this was for testing - now we need to make it right
    public void ClearGames() => Games = new Dictionary<int, Game>();

    //todo if null do remove instead of assigning
    //todo make another method for non-testing purposes
    //for testing
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
    public void SetGame(Game? game) => Games[game.Id] = game;
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

    public bool StartPlaying(int userId)
    {
        var game = Games.Values.FirstOrDefault(x => !x.SecondUserId.HasValue);
        //todo tdd ensure id uniqueness
        if (game is null)
        {
            var newGame = new Game(userId);
            Games[newGame.Id] = newGame;
            return false;
        }
        else
        {
            game.Start(userId);
            return true;
        }
    }

    public Dictionary<int, Game> Games { get; private set; } = new Dictionary<int, Game>();
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

    public override string ToString() => $"[{x},{y}]";

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
    public bool IsDestroyed => Decks.Values.All(x => x.Destroyed);

    //todo make it a hashset
    public Dictionary<Cell, Deck> Decks { get; set; } = new Dictionary<Cell, Deck>();

    public override string ToString() => "(" + string.Join(";", Decks.Keys) + ")";
}