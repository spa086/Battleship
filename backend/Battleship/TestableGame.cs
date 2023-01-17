using BattleShipLibrary;

namespace Battleship;

class TestableGame : Game
{
    public TestableGame(int sessionId) : base(sessionId)
    {

    }

    public List<int> ExcludedLocations1 => excludedLocations1;
    public List<int> ExcludedLocations2 => excludedLocations2;
    public bool Win => win;
    public bool Player1Turn => player1Turn;
    public List<Ship>? Player1Ships => player1Ships;
    public List<Ship>? Player2Ships => player2Ships;

    public Game SetState(GameState newState)
    {
        State = newState;
        return this;
    }

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

    public void SetupSimpleFleets(int[]? deckLocations1,
        int[]? deckLocations2)
    {
        player1Ships = CreateSimpleFleet(deckLocations1);
        player2Ships = CreateSimpleFleet(deckLocations2);
    }

    private static List<Ship>? CreateSimpleFleet(int[]? deckLocations)
    {
        if (deckLocations is null)
            return null;
        var decks = deckLocations.Select(x => new Deck(x)).ToDictionary(x => x.Location);
        return new() {new Ship {Decks = decks}};
    }

    private static List<int> CreateLocationList(params int[] locations) => locations.ToList();
}


