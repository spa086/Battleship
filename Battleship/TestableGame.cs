using BattleshipLibrary;

namespace BattleshipTests;

public class TestableGame : Game
{
    public TestableGame(int userId) : base(userId)
    {

    }

    public TestableGame SetFirstUserId(int? firstUserId = null)
    {
        FirstUserId= firstUserId;
        return this;
    }

    public TestableGame SetSecondUserId(int? secondUserId = null)
    {
        SecondUserId = secondUserId;
        return this;
    }

    public Game SetState(GameState newState)
    {
        State = newState;
        return this;
    }

    public void SetupExcludedLocations(int userId, params Cell[] locations)
    {
        if (userId == FirstUserId) excludedLocations1 = CreateLocationList(locations);
        else if (userId == SecondUserId) excludedLocations2 = CreateLocationList(locations);
        else throw new Exception("Incorrect userId");
    }

    //todo check for 3 times
    public void SetTurn(bool setPlayer1Turn) => 
        State = setPlayer1Turn ? GameState.Player1Turn : GameState.Player2Turn;

    public void StandardSetup()
    {
        excludedLocations1 = CreateLocationList();
        excludedLocations2 = CreateLocationList();
        State = GameState.Player1Turn;
        win = false;
        SetupSimpleFleets(new[] { new Cell(1,1) }, 1,  
            new[] { new Cell(3, 3) }, 2);
    }

    public void SetupFleets(IEnumerable<Ship> fleet1, IEnumerable<Ship> fleet2)
    {
        firstFleet = fleet1.ToList();
        secondFleet = fleet2.ToList();
    }

    public void SetupSimpleFleets(Cell[]? deckLocations1, int? firstUserId,
        Cell[]? deckLocations2, int? secondUserId)
    {
        firstFleet = CreateSimpleFleet(deckLocations1);
        FirstUserId = firstUserId;
        secondFleet = CreateSimpleFleet(deckLocations2);
        SecondUserId = secondUserId;
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