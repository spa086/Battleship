using BattleshipLibrary;

namespace BattleshipTests;

public class TestableGame : Game
{
    public TestableGame(int userId) : base(userId)
    {

    }

    public TimerPlus? Timer => turnTimer;

    public TestableGame SetSecondUserId(int? secondUserId = null)
    {
        GuestId = secondUserId;
        return this;
    }

    public TestableGame SetState(GameState newState)
    {
        State = newState;
        return this;
    }

    public TimerPlus? GetTimer() => turnTimer;

    public int? SetupTurnTime { get; set; }

    public void SetupExcludedLocations(int userId, params Cell[] locations)
    {
        if (userId == HostId) hostExcludedLocations = CreateLocationList(locations);
        else if (userId == GuestId) guestExcludedLocations = CreateLocationList(locations);
        else throw new Exception("Incorrect userId");
    }

    //todo check for 3 times
    public void SetTurn(bool setPlayer1Turn) => 
        State = setPlayer1Turn ? GameState.HostTurn : GameState.GuestTurn;

    public void StandardSetup()
    {
        hostExcludedLocations = CreateLocationList();
        guestExcludedLocations = CreateLocationList();
        State = GameState.HostTurn;
        SetupTurnTime = 30;
        SetupSimpleFleets(new[] { new Cell(1,1) }, 1,  
            new[] { new Cell(3, 3) }, 2);
    }

    public void SetupFleets(IEnumerable<Ship> fleet1, IEnumerable<Ship> fleet2)
    {
        hostFleet = fleet1.ToArray();
        guestFleet = fleet2.ToArray();
    }

    public void DestroyFleet(int userId)
    {
        if(userId == HostId)
            foreach (var ship in HostFleet!)
                foreach (var deck in ship.Decks.Values)
                    deck.Destroyed = true;
    }

    public void SetupNewTurn(int secondsLeft) => RenewTurnTimer(secondsLeft);

    public void SetupUserName(int userId, string? userName)
    {
        if (userId == HostId) HostName = userName;
        else if (userId == GuestId) GuestName = userName;
        else throw new Exception($"User [{userId}] is not found.");
    }
        

    public void SetupSimpleFleets(Cell[]? deckLocations1, int firstUserId,
        Cell[]? deckLocations2, int? secondUserId)
    {
        hostFleet = CreateSimpleFleet(deckLocations1);
        HostId = firstUserId;
        guestFleet = CreateSimpleFleet(deckLocations2);
        GuestId = secondUserId;
    }

    protected override void RenewTurnTimer(int secondsLeft = 30) => 
        RenewTimerInternal(SetupTurnTime ?? secondsLeft);

    private static Ship[]? CreateSimpleFleet(Cell[]? deckLocations)
    {
        if (deckLocations is null)
            return null;
        var decks = deckLocations.Select(location => new Deck(location.x, location.y))
            .ToDictionary(x => x.Location);
        return new[] {new Ship {Decks = decks}};
    }

    private static List<Cell> CreateLocationList(params Cell[] locations) => locations.ToList();
}