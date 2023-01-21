using System.Diagnostics.CodeAnalysis;

namespace BattleShipLibrary;

//todo use DI instead
public static class GamePool
{
    //for testing
    public static void SetGame(Game? game) => TheGame = game;

    public static bool StartPlaying(int userId)
    {
        if (TheGame is null)
        {
            TheGame = new Game(userId);
        }
        else
        {
            TheGame.Start();
        }

        return TheGame is not null;
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

    public override string ToString()
    {
        var result = $"{x},{y}";
        return result;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        var otherLocation = (Cell)obj!;
        return otherLocation.x == x && otherLocation.y == y;
    }

    public override int GetHashCode()
    {
        return x * 100 + y;
    }

    public static bool operator ==(Cell cell1, Cell cell2)
    {
        var result = cell1.Equals(cell2);
        return result;
    }

    public static bool operator !=(Cell cell1, Cell cell2)
    {
        var result = cell1.Equals(cell2);
        return result;
    }
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

public enum GameState
{
    WaitingForPlayer2,
    BothPlayersCreateFleets,
    WaitingForPlayer2ToCreateFleet,
    Player1Turn,
    Player2Turn
}

public class Game
{
    public Game(int user1Id)
    {
        FirstUserId = user1Id;
    }

    public int? FirstUserId { get; private set; }
    public int? SecondUserId { get; private set; }

    public GameState State { get; protected set; }

    public List<Cell> ExcludedLocations1 => excludedLocations1;
    public List<Cell> ExcludedLocations2 => excludedLocations2;
    public bool Win => win;
    public List<Ship>? Player1Ships => player1Ships;
    public List<Ship>? Player2Ships => player2Ships;

    //todo test
    public void Start() => State = GameState.BothPlayersCreateFleets;


    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.x, deckLocation.y))
                .ToDictionary(x => x.Location)
        }).ToList();
        var tempPlayer1Ships = userId == FirstUserId ? newShips : player1Ships;
        var tempPlayer2Ships = userId == FirstUserId ? player2Ships : newShips;
        var player1Decks = (tempPlayer1Ships ?? Array.Empty<Ship>().ToList())
            .SelectMany(x => x.Decks.Keys).ToHashSet();
        var player2Decks = (tempPlayer2Ships ?? Array.Empty<Ship>().ToList())
            .SelectMany(x => x.Decks.Keys).ToHashSet();
        if (player1Decks.Intersect(player2Decks).Any())
            throw new Exception("Two ships at the same location.");
        UpdateState(userId, newShips);
    }

    private void UpdateState(int userId, List<Ship> newShips)
    {
        if (userId == FirstUserId)
        {
            player1Ships = newShips;
            State = GameState.WaitingForPlayer2ToCreateFleet;
        }
        else
        {
            player2Ships = newShips;
            State = GameState.Player1Turn;
        }
    }

    public AttackResult Attack(int userId, Cell attackedLocation)
    {
        //todo tdd that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo tdd this condition
        //todo check for 3 times
        var player1Turn = State == GameState.Player1Turn;
        var attackedShips = player1Turn ?
            player2Ships!.Where(x => !IsDestroyed(x)) : player1Ships!.Where(x => !IsDestroyed(x));
        var result = AttackResult.Missed;
        var attackedShip = GetAttackedShip(attackedLocation, attackedShips);
        ProcessHit(attackedLocation, attackedShip, ref result);
        ProcessWin(player1Turn, attackedShips, ref result);
        return result; //todo tdd correct result
    }

    private void ProcessWin(bool player1Turn, IEnumerable<Ship> attackedShips, ref AttackResult result)
    {
        if (attackedShips.All(x => IsDestroyed(x)))
        {
            win = true;
            result = AttackResult.Win;
        }
        else State = player1Turn ? GameState.Player2Turn : GameState.Player1Turn; //todo tdd this
    }

    private static void ProcessHit(Cell attackedLocation, Ship? attackedShip, ref AttackResult result)
    {
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            if (attackedShip.Decks.All(x => x.Value.Destroyed)) result = AttackResult.Killed;
            else result = AttackResult.Hit;
        }
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship => 
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private void Exclude(Cell location)
    {
        //todo check for 3 times
        var currentExcluded = State == GameState.Player1Turn ? excludedLocations1 : excludedLocations2;
        if (currentExcluded.Contains(location)) 
            throw new Exception($"Location [{location}] is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<Cell> excludedLocations1 = new();
    protected List<Cell> excludedLocations2 = new();
    //todo tdd validate ship shape
    protected List<Ship>? player1Ships;
    protected List<Ship>? player2Ships;
    protected bool win;

    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);
}

public enum AttackResult
{
    Hit,
    Killed,
    Missed,
    Win
}