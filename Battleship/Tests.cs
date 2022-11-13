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
    //todo tdd 2nd player turn
    //todo tdd trying to attack the same place twice (not a valid turn)
    //todo tdd field borders (and what is nowhere left to fire?)
    //todo tdd 2nd dimension
    //todo ASP project
    //todo tdd multidecked ships (what about zigzag ships in 2 dimensions?)

    [SetUp]
    public void SetUp()
    {
        excludedLocations1 = CreateLocationList();
        player1Turn = true;
        win = false;
    }

    //todo similar for 2nd player
    [Test]
    public void Miss()
    {
        CreateShips(1, 2);

        Attack(0);

        Assert.False(player1Turn);
        Assert.False(win);
        Assert.That(player2Ships.Count, Is.EqualTo(1));
    }

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        CreateShips(1, 2);

        Attack(144);

        Assert.That(excludedLocations1.Count, Is.EqualTo(1));
        Assert.That(excludedLocations1.Single(), Is.EqualTo(144));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackAndWin()
    {
        CreateShips(1, 2);

        Attack(2);

        Assert.That(player1Ships.Count, Is.EqualTo(1));
        Assert.That(player2Ships.Count, Is.Zero);
        Assert.That(win);
        Assert.That(player1Turn);
    }

    private void Attack(int attackedLocation)
    {
        excludedLocations1.Add(attackedLocation);
        if (player2Ships.Contains(attackedLocation)) win = true;
        else player1Turn = false;
        player2Ships.Remove(attackedLocation);
    }

    private List<int> CreateLocationList(params int[] locations) => 
        locations.ToList();

    private void CreateShips(int ship1Location, int ship2Location)
    {
        player1Ships = CreateLocationList(ship1Location);
        player2Ships = CreateLocationList(ship2Location);
    }

    private bool player1Turn = true;
    private bool win;
    private List<int> excludedLocations1;
    private List<int> excludedLocations2;
    private List<int> player1Ships;
    private List<int> player2Ships;
}