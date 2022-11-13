namespace Battleship;

//lines 80 chars
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    //todo tdd miss
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if two ships in same location
    //todo tdd throw if ships are adjacent

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        player1Turn = true;
        excludedLocations1 = CreateLocationList();

        Hit(144);

        Assert.That(excludedLocations1.Count, Is.EqualTo(1));
        Assert.That(excludedLocations1.Single(), Is.EqualTo(144));
    }

    [Test]
    public void AttackAndWin()
    {
        player1Turn = true;
        excludedLocations1 = CreateLocationList();
        player1Ships = CreateLocationList(1);
        player2Ships = CreateLocationList(2);

        Hit(2);

        Assert.That(player1Ships.Count, Is.EqualTo(1));
        Assert.That(player1Ships.Single(), Is.EqualTo(1));
        Assert.That(player2Ships.Count, Is.Zero);
        Assert.That(hit);
        Assert.That(win);
        Assert.That(player1Turn);
    }

    private void Hit(int attackedLocation)
    {
        excludedLocations1.Add(attackedLocation);
        player2Ships.Remove(attackedLocation);
        hit = true;
        win = true;
    }

    private List<int> CreateLocationList(params int[] locations) => 
        locations.ToList();

    private bool player1Turn = true;
    private bool win;
    private bool hit;
    private List<int> excludedLocations1;
    private List<int> excludedLocations2;
    private List<int> player1Ships;
    private List<int> player2Ships;
}