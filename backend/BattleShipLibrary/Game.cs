namespace BattleShipLibrary;

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

    //todo test
    public void Start() => State = GameState.BothPlayersCreateFleets;


    public void CreateAndSaveShips(FleetCreationModel model)
    {
        if ((player1Ships ?? Array.Empty<Ship>().ToList()).ToHashSet().Union(
            (player2Ships ?? Array.Empty<Ship>().ToList()).ToHashSet()).Any())
            throw new Exception("Two ships at the same location.");
        var newShips = model.Ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Select(deckLocation => new Deck(deckLocation))
                .ToDictionary(x => x.Location)
        }).ToList();
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

    public void Attack(int attackedLocation)
    {
        //todo tdd that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo tdd this condition
        //todo check for 3 times
        var player1Turn = State == GameState.Player1Turn;
        var attackedShips = player1Turn ? 
            player2Ships!.Where(x => !IsDestroyed(x)) : player1Ships!.Where(x => !IsDestroyed(x));
        //todo tdd this condition
        var attackedShip = attackedShips
            .SingleOrDefault(ship => ship.Decks.Values.Any(deck => deck.Location == attackedLocation));
        if (attackedShip is not null)
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
        var newState = player1Turn ? GameState.Player2Turn : GameState.Player1Turn;
        if (attackedShips.All(x => IsDestroyed(x))) win = true;
        else State = newState; //todo tdd this
    }

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