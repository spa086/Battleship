namespace Battleship;

class TestableGame : Game
{
    //todo do we need to expose all these propeties?
    public List<int> ExcludedLocations1 => excludedLocations1;
    public List<int> ExcludedLocations2 => excludedLocations2;
    public bool Win => win;
    public bool Player1Turn => player1Turn;
    public List<Ship> Player1Ships => this.player1Ships;
    public List<Ship> Player2Ships => this.player2Ships;

    public void SetupExcludedLocations(params int[] locations) => 
        excludedLocations1 = CreateLocationList(locations);

    public void SetTurn(bool setPlayer1Turn) => player1Turn = setPlayer1Turn;

    public void StandardSetup()
    {
        excludedLocations1 = CreateLocationList();
        excludedLocations2 = CreateLocationList();
        player1Turn = true;
        win = false;
        SetupSimpleFleets(new[] { 1 }, new[] { 2 });
    }

    public void SetupSimpleFleets(int[] deckLocations1,
        int[] deckLocations2)
    {
        player1Ships = CreateSimpleFleet(deckLocations1);
        player2Ships = CreateSimpleFleet(deckLocations2);
    }

    private static List<Ship> CreateSimpleFleet(int[] deckLocations) =>
        new()
        {
            new Ship
            {
                Decks = deckLocations.Select(x =>
                    new Deck(x)).ToDictionary(x => x.Location)
            }
        };

    private static List<int> CreateLocationList(params int[] locations) =>
        locations.ToList();
}

class Game
{
    public void CreateAndSaveShips(ShipsCreationFrontModel model)
    {
        var newShips = model.Ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Select(deckLocation =>
                new Deck(deckLocation)).ToDictionary(x => x.Location)
        }).ToList();
        if (model.ForPlayer1)
            player1Ships = newShips;
        //todo test for player 2
    }

    public void Attack(int attackedLocation)
    {
        Exclude(attackedLocation);
        var attackedShips =
            //todo tdd this condition
            player1Turn ? player2Ships.Where(x => !IsDestroyed(x))
            : player1Ships.Where(x => !IsDestroyed(x));
        //todo tdd this condition
        var attackedShip = attackedShips.SingleOrDefault(ship =>
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));
        if (attackedShip is not null)
            attackedShip.Decks.Values.Single(x =>
                x.Location == attackedLocation).Destroyed = true;
        if (attackedShips.All(x => IsDestroyed(x))) win = true;
        else player1Turn = !player1Turn; //todo tdd this
    }

    private void Exclude(int location)
    {
        var currentExcluded =
            player1Turn ? excludedLocations1 : excludedLocations2;
        if (currentExcluded.Contains(location))
            throw new Exception(
                $"Location [{location}] is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<int> excludedLocations1 = new();
    protected List<int> excludedLocations2 = new();
    //todo INPRO tdd validate ship shape
    protected List<Ship> player1Ships = new();
    protected List<Ship> player2Ships = new();
    protected bool player1Turn;
    protected bool win;

    public static bool IsDestroyed(Ship ship) =>
        ship.Decks.Values.All(x => x.Destroyed);
}

class Deck
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

class Ship
{
    //todo make it a hashset
    public Dictionary<int, Deck> Decks { get; set; }
        = new Dictionary<int, Deck>();
}

class ShipsCreationFrontModel
{
    public bool ForPlayer1 { get; set; }

    public ShipFrontModel[] Ships { get; set; } = Array.Empty<ShipFrontModel>();
}

class ShipFrontModel
{
    public int[] Decks { get; set; } = Array.Empty<int>();
}


