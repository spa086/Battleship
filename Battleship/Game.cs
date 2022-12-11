using BattleShipLibrary;

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


