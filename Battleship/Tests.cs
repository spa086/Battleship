namespace Battleship;

//lines 80 chars
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    [Test]
    public void SimplestHit()
    {
        var shipLocation = 0;
        var attackedLocation = 0;
        var hit = shipLocation == attackedLocation;
        Assert.That(hit);
    }
}