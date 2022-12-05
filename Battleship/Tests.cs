namespace Battleship;

//lines 80 chars
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if two ships in same location
    //todo tdd throw if ships are adjacent
    //todo tdd game cycle
    //todo tdd field borders (and what if nowhere left to fire?)
    //todo tdd 2nd dimension
    //todo ASP project
    //todo console interface

    [SetUp]
    public void SetUp()
    {
        excludedLocations1 = CreateLocationList();
        excludedLocations2 = CreateLocationList();
        player1Turn = true;
        win = false;
        SetupSimpleFleets(new[] { 1 }, new[] { 2 });
    }

    [Test]
    public void CreateShipsSimple()
    {
        Assert.DoesNotThrow(() => CreateShips(new ShipsCreationFrontModel
        {
            Ships = new[] { new ShipFrontModel
        {
            Decks =  new []{1,2}
           }}
        }));
    }

    [Test]
    public void DamagingAMultideckShip()
    {
        SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        player1Turn = false;

        Attack(1);

        Assert.That(player1Ships.AssertSingle().Decks[1].Destroyed);
    }

    //todo tdd this but for 1st player turn
    [Test]
    public void DestroyingAMultideckShip()
    {
        SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        player1Ships.Single().Decks[1].Destroyed = true;
        player1Turn = false;

        Attack(0);

        var destroyedShip = player1Ships.AssertSingle();
        Assert.That(destroyedShip.Decks.Values.All(x => x.Destroyed));
        Assert.That(IsDestroyed(destroyedShip));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackSamePlaceTwice()
    {
        excludedLocations1 = CreateLocationList(1);

        var exception = Assert.Throws<Exception>(() => Attack(1));
        Assert.That(exception.Message, 
            Is.EqualTo("Location [1] is already excluded."));
    }

    //todo similar for 2nd player
    [Test]
    public void Miss()
    {
        Attack(0);

        Assert.That(player1Turn, Is.False);
        Assert.That(win, Is.False);
        player2Ships.AssertSingle();
    }

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        Attack(144);

        Assert.That(excludedLocations1.AssertSingle(), Is.EqualTo(144));
    }

    [Test]
    public void AttackAndWin()
    {
        Attack(2);

        excludedLocations1.AssertSingle();
        Assert.That(IsDestroyed(player2Ships.AssertSingle()));
        Assert.That(win);
        Assert.That(player1Turn);
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        player1Turn = false;

        Attack(1);

        player2Ships.AssertSingle();
        Assert.That(IsDestroyed(player1Ships.AssertSingle()));
        Assert.That(win);
        Assert.That(player1Turn, Is.False);
    }

    private void Attack(int attackedLocation)
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

    private void CreateShips(ShipsCreationFrontModel model)
    {

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

    private void SetupSimpleFleets(int[] deckLocations1, 
        int[] deckLocations2)
    {
        player1Ships = CreateSimpleFleet(deckLocations1);
        player2Ships = CreateSimpleFleet(deckLocations2);
    }

    private bool player1Turn;
    private bool win;
    private List<int> excludedLocations1 = new();
    private List<int> excludedLocations2 = new();
    //todo INPRO tdd validate ship shape
    private List<Ship> player1Ships = new();
    private List<Ship> player2Ships = new();

    private static List<int> CreateLocationList(params int[] locations) =>
        locations.ToList();

    private static List<Ship> CreateSimpleFleet(int[] deckLocations) => 
        new()
        {
            new Ship
            {
                Decks = deckLocations.Select(x =>
                    new Deck(x)).ToDictionary(x => x.Location)
            }
        };

    //test helper
    private static bool IsDestroyed(Ship ship) => 
        ship.Decks.Values.All(x => x.Destroyed);

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
        public Dictionary<int, Deck> Decks { get; set; } 
            = new Dictionary<int, Deck>();
    }

    class ShipsCreationFrontModel
    {
        public ShipFrontModel[] Ships { get; set; } = Array.Empty<ShipFrontModel>();
    }

    class ShipFrontModel
    {
        public int[] Decks { get; set; } = Array.Empty<int>();
    }
}

public static class Extensions
{
    //todo tdd what if it is null
    public static T AssertSingle<T>(this IEnumerable<T> collection)
    {
        Assert.That(collection.Count(), Is.EqualTo(1));
        return collection.Single();
    }
}