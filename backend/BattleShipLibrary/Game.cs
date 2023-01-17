using System.Security.Cryptography.X509Certificates;

namespace BattleShipLibrary;

//todo use DI instead
public static class GamePool
{
    //todo make ForTesting attribute
    //for testing
    public static void ClearGames() => Games.Clear();

    //for testing
    public static void SetGame(Game game, int sessionId) => Games[sessionId] = game;

    public static bool StartPlaying(int sessionId)
    {
        var gameAlreadyExisted = Games.ContainsKey(sessionId);
        if (gameAlreadyExisted)
        {
            var game = Games[sessionId];
            game.Start();
        }
        else Games[sessionId] = new Game(sessionId);
        return gameAlreadyExisted;
    }

    //todo does it need to be public?
    public static Dictionary<int, Game> Games { get; private set; } = new Dictionary<int, Game>();
}

public class FleetCreationModel
{
    public bool IsForPlayer1 { get; set; }

    public ShipCreationModel[] Ships { get; set; } = Array.Empty<ShipCreationModel>();
}

public class ShipCreationModel
{
    public int[] Decks { get; set; } = Array.Empty<int>();
}

public class Deck
{
    //todo tdd this
    public Deck(int location, bool destroyed = false)
    {
        Destroyed = destroyed;
        Location = location;
    }

    public bool Destroyed { get; set; }

    public int Location { get; set; }
}

public class Ship
{
    //todo make it a hashset
    public Dictionary<int, Deck> Decks { get; set; } = new Dictionary<int, Deck>();
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
    public Game(int sessionId)
    {
        UserId = sessionId;
    }

    public int UserId { get; private set; }

    public GameState State { get; protected set; }

    public List<int> ExcludedLocations1 => excludedLocations1;
    public List<int> ExcludedLocations2 => excludedLocations2;
    public bool Win => win;
    public List<Ship>? Player1Ships => player1Ships;
    public List<Ship>? Player2Ships => player2Ships;

    //todo test
    public void Start() => State = GameState.BothPlayersCreateFleets;


    public void CreateAndSaveShips(FleetCreationModel model)
    {
        var newShips = model.Ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Select(deckLocation => new Deck(deckLocation))
                .ToDictionary(x => x.Location)
        }).ToList();
        var tempPlayer1Ships = model.IsForPlayer1 ? newShips : player1Ships;
        var tempPlayer2Ships = model.IsForPlayer1 ? player2Ships : newShips;
        var player1Decks = (tempPlayer1Ships ?? Array.Empty<Ship>().ToList())
            .SelectMany(x => x.Decks.Keys).ToHashSet();
        var player2Decks = (tempPlayer2Ships ?? Array.Empty<Ship>().ToList())
            .SelectMany(x => x.Decks.Keys).ToHashSet();
        if (player1Decks.Intersect(player2Decks).Any())
            throw new Exception("Two ships at the same location.");
        UpdateState(model, newShips);
    }

    private void UpdateState(FleetCreationModel model, List<Ship> newShips)
    {
        if (model.IsForPlayer1)
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

    public AttackResult Attack(int attackedLocation)
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

    private static void ProcessHit(int attackedLocation, Ship? attackedShip, ref AttackResult result)
    {
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            if (attackedShip.Decks.All(x => x.Value.Destroyed)) result = AttackResult.Killed;
            else result = AttackResult.Hit;
        }
    }

    private static Ship? GetAttackedShip(int attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship => 
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private void Exclude(int location)
    {
        //todo check for 3 times
        var currentExcluded = State == GameState.Player1Turn ? excludedLocations1 : excludedLocations2;
        if (currentExcluded.Contains(location)) 
            throw new Exception($"Location [{location}] is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<int> excludedLocations1 = new();
    protected List<int> excludedLocations2 = new();
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