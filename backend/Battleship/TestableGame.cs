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

    public void SetupExcludedLocations(params int[] locations) => 
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