using NLog;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BattleshipLibrary;

public interface IAi
{
    Ship[] GenerateShips();

    Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, IEnumerable<Cell> excludedLocations);
}

//todo use DI instead
public static class Log
{
    public static readonly ILogger ger = LogManager.GetCurrentClassLogger();
}

public enum GameState
{
    WaitingForGuest,
    BothPlayersCreateFleets,
    OnePlayerCreatesFleet,
    HostTurn,
    GuestTurn,
    HostWon,
    GuestWon,
    Cancelled
}

public enum AttackResult
{
    Hit,
    Missed,
    Win
}

//todo inherit TestableGamePool
public class GamePool
{
    private readonly IAi ai;

    public GamePool(IAi ai)
    {
        this.ai = ai;
    }

    //for testing
    public int? SetupMatchingTimeSeconds { get; set; }

    public Game? GetGame(int userId)
    {
        var gamesByUserId = Games.Values.Where(x => 
            x.Host.Id == userId || x.Guest?.Id == userId);
        if (gamesByUserId.Count() > 1)
            throw new Exception($"User id = [{userId}] participates in several games. Game id's: " +
                $"[{string.Join(", ", gamesByUserId.Select(x => x.Id))}].");
        return gamesByUserId.SingleOrDefault();
    }

    //for testing
    public void ClearGames() => Games.Clear();

    //todo if null do remove instead of assigning
    //todo make another method for non-testing purposes
    //for testing
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
    public void AddGame(Game? game) => Games[game.Id] = game;
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

    public bool StartPlaying(int userId)
    {
        var game = Games.Values.FirstOrDefault(x => x.Guest is null);
        //todo tdd ensure id uniqueness
        if (game is null)
        {
            var newGame = new Game(userId, ai, SetupMatchingTimeSeconds ?? 30);
            Games[newGame.Id] = newGame;
            return false;
        }
        else
        {
            game.Start(userId);
            return true;
        }
    }

    //todo make it private, get in tests some other way,
    //maybe some for-testing method in GamePool or TestGamePool here
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