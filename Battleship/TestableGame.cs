using BattleshipLibrary;

namespace BattleshipTests;

public class TestableGame : Game
{
    public TestableGame(int userId, int matchingSeconds = 30) : base(userId, new TestAi(), matchingSeconds)
    {
    }

    public TimerWithDueTime? Timer => TheTimer;

    public TestableGame CreateGuest(int? guestId = null)
    {
        if (guestId is not null)
        {
            if (Guest is null) Guest = new User();
            Guest.Id = guestId.Value;
        }

        return this;
    }

    public TestableGame SetState(GameState newState)
    {
        State = newState;
        return this;
    }

    public TimerWithDueTime? GetTimer() => TheTimer;

    public int? SetupTurnTime { get; set; }
    public int? SetupMatchingTime { get; set; }

    public void EnqueueAiAttackLocation(Cell location) =>
        (Ai as TestAi)!.AttackLocationsQueue.Enqueue(location);

    public void SetupExcludedLocations(int userId, params Cell[] locations)
    {
        if (userId == Host.Id) Host.ExcludedLocations = CreateLocationList(locations);
        else if (userId == Guest!.Id) Guest.ExcludedLocations = CreateLocationList(locations);
        else throw new Exception("Incorrect userId");
    }

    //todo check for 3 times
    public void SetTurn(bool setPlayer1Turn) =>
        State = setPlayer1Turn ? GameState.HostTurn : GameState.GuestTurn;

    public void StandardSetup()
    {
        Host.ExcludedLocations = CreateLocationList();
        Guest = new User { ExcludedLocations = CreateLocationList() };
        State = GameState.HostTurn;
        SetupTurnTime = 30;
        SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(3, 3) }, 2);
    }

    public void SetupFleets(IEnumerable<Ship>? hostFleet, IEnumerable<Ship>? guestFleet)
    {
        Host.Fleet = hostFleet?.ToArray();
        Guest!.Fleet = guestFleet?.ToArray();
    }

    public void DestroyFleet(int userId)
    {
        if (userId == Host.Id)
            foreach (var ship in Host.Fleet!)
            foreach (var deck in ship.Decks.Values)
                deck.Destroyed = true;
    }

    public void SetupBattleTimer(int secondsLeft) => SetBattleTimer(secondsLeft);
    public void SetupShipsCreationTimer(int secondsLeft) => SetShipsCreationTimer(secondsLeft);
    public void SetupMatchingTimer(int secondsLeft) => SetShipsCreationTimer(secondsLeft);

    public void SetupUserName(int userId, string? userName)
    {
        if (userId == Host.Id) Host.Name = userName;
        else if (userId == Guest!.Id) Guest.Name = userName;
        else throw new Exception($"User [{userId}] is not found.");
    }

    //todo move id setups to different method/property
    public void SetupSimpleFleets(Cell[]? hostDeckLocations, int? hostId = null,
        Cell[]? guestDeckLocations = null, int? guestId = null)
    {
        Host!.Fleet = CreateSimpleFleet(hostDeckLocations);
        Host.Id = hostId ?? Host.Id;
        Guest!.Fleet = CreateSimpleFleet(guestDeckLocations);
        if (guestId is null) Guest = null;
        else Guest.Id = guestId.Value;
    }

    protected override void SetBattleTimer(int secondsLeft = 30) =>
        base.SetBattleTimer(SetupTurnTime ?? secondsLeft);

    protected override void SetShipsCreationTimer(int secondsLeft = 30) =>
        base.SetShipsCreationTimer(SetupTurnTime ?? secondsLeft);

    protected override void SetMatchingTimer(int secondsLeft = 30) =>
        base.SetMatchingTimer(SetupMatchingTime ?? secondsLeft);

    private static Ship[]? CreateSimpleFleet(Cell[]? deckLocations)
    {
        if (deckLocations is null)
            return null;
        var decks = deckLocations.Select(location => new Deck(location.X, location.Y))
            .ToDictionary(x => x.Location);
        return new[] { new Ship { Decks = decks } };
    }

    private static List<Cell> CreateLocationList(params Cell[] locations) => locations.ToList();
}