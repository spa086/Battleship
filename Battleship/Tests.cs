namespace Battleship;

//lines 80 chars
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    [TestCase(0, 0, true)]
    [TestCase(1, 0, false)]
    [TestCase(0, 1, false)]
    public void AttackingAndExcluding(int shipLocation, int attackedLocation, 
        bool expectedAnswer)
    {
        var excludedLocations = new List<int>();
        
        var hit = shipLocation == attackedLocation;
        excludedLocations.Add(attackedLocation);

        Assert.That(hit, Is.EqualTo(expectedAnswer));
        Assert.That(excludedLocations.Count, Is.EqualTo(1));
        Assert.That(excludedLocations.Single(), Is.EqualTo(attackedLocation));
    }
}