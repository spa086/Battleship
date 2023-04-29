using System.Diagnostics.CodeAnalysis;
using NLog;

namespace BattleshipLibrary;

public interface IAi
{
    Ship[] GenerateShips();

    Cell ChooseAttackLocation(Ship[] enemyShips, IEnumerable<Cell> excludedLocations);
}

public static class Log
{
    // ReSharper disable once InconsistentNaming
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

public class GamePool
{
    private readonly IAi ai;
    private readonly IMatchingTime matchingTime;

    public GamePool(IAi ai, IMatchingTime matchingTime)
    {
        this.ai = ai;
        this.matchingTime = matchingTime;
    }

    public Game[] GetGames() => games.Values.ToArray();

    public Game? GetGame(int userId)
    {
        var gamesByUserId =
            games.Values.Where(x => x.Host.Id == userId || x.Guest?.Id == userId).ToArray();
        var nonFinishedGames = gamesByUserId.Where(x => !x.ItsOver).ToArray();
        if (nonFinishedGames.Length > 1)
            throw new Exception($"User id = [{userId}] participates in several games. Game id's: " +
                                $"[{string.Join(", ", gamesByUserId.Select(x => x.Id))}].");
        return nonFinishedGames.Any() ? nonFinishedGames.Single() : gamesByUserId.MaxBy(x => x.StartTime);
    }

    public void ClearGames() => games.Clear();

    public void AddGame(Game game) => games[game.Id] = game;

    public bool StartPlaying(int userId)
    {
        var allGames = games.Values;
        var ongoingGamesWithUser = allGames
            .Where(x => x.Host.Id == userId || x.Guest?.Id == userId).Where(x => !x.ItsOver).ToArray();
        if (ongoingGamesWithUser.Any())
            throw new Exception($"Can't start playing: you already participate in " +
                                $"ongoing game id=[{ongoingGamesWithUser.First().Id}].");
        var gameToJoin = allGames.FirstOrDefault(x => x.Guest is null);
        if (gameToJoin is null)
        {
            var newGame = new Game(userId, ai, matchingTime.Seconds());
            games[newGame.Id] = newGame;
            return false;
        }

        gameToJoin.Start(userId);
        return true;
    }

    private readonly Dictionary<int, Game> games = new();
}

public class MatchingTime : IMatchingTime
{
    public int Seconds() => 30;
}

public interface IMatchingTime
{
    int Seconds();
}

public readonly struct Cell
{
    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }

    public readonly int X;
    public readonly int Y;

    public override string ToString() => $"[{X},{Y}]";

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        var otherLocation = (Cell)obj!;
        return otherLocation.X == X && otherLocation.Y == Y;
    }

    public override int GetHashCode() => X * 100 + Y;

    public static bool operator ==(Cell cell1, Cell cell2) => cell1.Equals(cell2);

    public static bool operator !=(Cell cell1, Cell cell2) => cell1.Equals(cell2);
}

public class Deck
{
    public Deck(int x, int y, bool destroyed = false)
    {
        Destroyed = destroyed;
        Location = new Cell(x, y);
    }

    public bool Destroyed { get; set; }
    
    public Cell Location { get; }
}

public class Ship
{
    public bool IsDestroyed => Decks.Values.All(x => x.Destroyed);

    public Dictionary<Cell, Deck> Decks { get; init; } = new();

    public override string ToString() => "(" + string.Join(";", Decks.Keys) + ")";
}