using BattleShipLibrary;

namespace BattleshipTests;

public class TestableGame : Game
{
    public TestableGame(int sessionId) : base(sessionId)
    {

    }

    public Game SetState(GameState newState)
    {
        State = newState;
        return this;
    }

    public void SetupExcludedLocations(params Cell[] locations) => 
        excludedLocations1 = CreateLocationList(locations);

    //todo check for 3 times
    public void SetTurn(bool setPlayer1Turn) => 
        State = setPlayer1Turn ? GameState.Player1Turn : GameState.Player2Turn;

    public void StandardSetup()
    {
        excludedLocations1 = CreateLocationList();
        excludedLocations2 = CreateLocationList();
        State = GameState.Player1Turn;
        win = false;
        SetupSimpleFleets(new[] { new Cell(1,1) }, new[] { new Cell(3, 3) });
    }

    public void SetupFleets(IEnumerable<Ship> fleet1, IEnumerable<Ship> fleet2)
    {
        player1Ships = fleet1.ToList();
        player2Ships = fleet2.ToList();
    }

    public void SetupSimpleFleets(Cell[]? deckLocations1,
        Cell[]? deckLocations2)
    {
        player1Ships = CreateSimpleFleet(deckLocations1);
        player2Ships = CreateSimpleFleet(deckLocations2);
    }

    private static List<Ship>? CreateSimpleFleet(Cell[]? deckLocations)
    {
        if (deckLocations is null)
            return null;
        var decks = deckLocations.Select(location => new Deck(location.x, location.y))
            .ToDictionary(x => x.Location);
        return new() {new Ship {Decks = decks}};
    }

    private static List<Cell> CreateLocationList(params Cell[] locations) => locations.ToList();
}