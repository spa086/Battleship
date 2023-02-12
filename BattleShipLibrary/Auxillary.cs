﻿using System.Diagnostics.CodeAnalysis;

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
    public static Game? GetGame(int userId)
    {
        //todo tdd throw if user participates in several games simultaneously
        var game = Games.Values.SingleOrDefault(x => 
            x.FirstUserId == userId || x.SecondUserId == userId);
        return game;
    }

    //for testing
    public static void ClearGames()
    {
        Games = new Dictionary<int, Game>();
    }

    //todo if null do remove instead of assigning
    //todo make another method for non-testing purposes
    //for testing
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
    public static void SetGame(Game? game) => Games[game.Id] = game;
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

    public static bool StartPlaying(int userId)
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

    public static Dictionary<int, Game> Games { get; private set; } = new Dictionary<int, Game>();
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