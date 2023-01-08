namespace BattleShipLibrary;

public static class GamePool
{
    //todo make ForTesting attribute
    //for testing
    public static void ClearGames() => Games.Clear();

    //for testing
    public static void SetGame(Game game, int sessionId) => Games[sessionId] = game;

    public static void StartPlaying(int sessionId)
    {
#pragma warning disable CA1854 
        if (Games.ContainsKey(sessionId))
        {
            var game = Games[sessionId];
            game.Start();
        }
        else Games[sessionId] = new Game(sessionId);
#pragma warning restore CA1854 
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

public class Game
{
    public Game(int sessionId)
    {
        UserId= sessionId;
    }

    public int UserId { get; private set; }

    public bool Started { get; protected set; }

    //todo test
    public void Start() => Started = true;


    public void CreateAndSaveShips(FleetCreationModel model)
    {
        var newShips = model.Ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Select(deckLocation => new Deck(deckLocation))
                .ToDictionary(x => x.Location)
        }).ToList();
        if (model.IsForPlayer1)
            player1Ships = newShips;
        //todo test for player 2
    }

    public void Attack(int attackedLocation)
    {
        Exclude(attackedLocation);
        //todo tdd this condition
        var attackedShips = player1Turn ? 
            player2Ships.Where(x => !IsDestroyed(x)) : player1Ships.Where(x => !IsDestroyed(x));
        //todo tdd this condition
        var attackedShip = attackedShips
            .SingleOrDefault(ship => ship.Decks.Values.Any(deck => deck.Location == attackedLocation));
        if (attackedShip is not null)
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
        if (attackedShips.All(x => IsDestroyed(x))) win = true;
        else player1Turn = !player1Turn; //todo tdd this
    }

    private void Exclude(int location)
    {
        var currentExcluded = player1Turn ? excludedLocations1 : excludedLocations2;
        if (currentExcluded.Contains(location)) 
            throw new Exception($"Location [{location}] is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<int> excludedLocations1 = new();
    protected List<int> excludedLocations2 = new();
    //todo INPRO tdd validate ship shape
    protected List<Ship> player1Ships = new();
    protected List<Ship> player2Ships = new();
    protected bool player1Turn;
    protected bool win;

    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);
}