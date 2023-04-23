using System.Diagnostics.CodeAnalysis;
using NLog;

namespace BattleshipLibrary;

public interface IAi
{
    Ship[] GenerateShips();

    Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, IEnumerable<Cell> excludedLocations);
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
        var gamesByUserId =
            Games.Values.Where(x => x.Host.Id == userId || x.Guest?.Id == userId).ToArray();
        var nonFinishedGames = gamesByUserId.Where(x => !x.ItsOver).ToArray();
        if (nonFinishedGames.Length > 1)
            throw new Exception($"User id = [{userId}] participates in several games. Game id's: " +
                                $"[{string.Join(", ", gamesByUserId.Select(x => x.Id))}].");
        return nonFinishedGames.Any() ? nonFinishedGames.Single() : gamesByUserId.MaxBy(x => x.StartTime);
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
        var games = Games.Values;
        var ongoingGamesWithUser = games
            .Where(x => x.Host.Id == userId || x.Guest?.Id == userId).Where(x => !x.ItsOver).ToArray();
        if (ongoingGamesWithUser.Any())
            throw new Exception($"Can't start playing: you already participate in " +
                                $"ongoing game id=[{ongoingGamesWithUser.First().Id}].");
        var gameToJoin = games.FirstOrDefault(x => x.Guest is null);
        //todo tdd ensure id uniqueness
        if (gameToJoin is null)
        {
            var newGame = new Game(userId, ai, SetupMatchingTimeSeconds ?? 30);
            Games[newGame.Id] = newGame;
            return false;
        }

        gameToJoin.Start(userId);
        return true;
    }

    //todo make it private, get in tests some other way,
    //maybe some for-testing method in GamePool or TestGamePool here
    public Dictionary<int, Game> Games { get; } = new();
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

    //todo make it a hashset
    public Dictionary<Cell, Deck> Decks { get; init; } = new();

    public override string ToString() => "(" + string.Join(";", Decks.Keys) + ")";
}