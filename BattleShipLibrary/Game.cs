using NLog;
using System.Timers;

namespace BattleshipLibrary;

public class Game
{
    public Game(int user1Id)
    {
        HostId = user1Id;
        var random = new Random();
        Id = random.Next();
    }

    //todo tdd this field
    public int Id { get; private set; }

    //todo make User class
    public int HostId { get; protected set; }
    //todo persist user name between games
    public string? HostName { get; set; }
    public int? GuestId { get; protected set; }
    //todo tdd this field in whatsup tests
    public string? GuestName { get; set; }

    public GameState State
    {
        get => state; 
        protected set
        {
            Log.ger.Info($"Game with id [{Id}] has changed state. Previous state: [{state}]. " +
                $"New State: [{value}].");
            state = value;
        }
    }
    public int? TurnSecondsLeft =>
        turnTimer is null ? null : (int)Math.Ceiling(turnTimer.DueTime.TotalMilliseconds / 1000f);

    public List<Cell> HostExcludedLocations => hostExcludedLocations;
    public List<Cell> GuestExcludedLocations => guestExcludedLocations;
    public Ship[]? HostFleet => hostFleet;
    public Ship[]? GuestFleet => guestFleet;

    public bool BattleOngoing => State == GameState.HostTurn || State == GameState.GuestTurn;
    public bool CreatingFleets =>
        State == GameState.BothPlayersCreateFleets || State == GameState.OnePlayerCreatesFleet;
    public bool ItsOver => State == GameState.HostWon || State == GameState.GuestWon;

    //todo tdd
    public void DisposeOfTimer()
    {
        turnTimer?.Dispose();
        turnTimer = null;
    }

    //todo tdd
    public void SetTechnicalWinner(bool player1Won)
    {
        DisposeOfTimer();
        State = player1Won ? GameState.HostWon : GameState.GuestWon;
    }

    //todo tdd
    public void Start(int secondUserId)
    {
        State = GameState.BothPlayersCreateFleets;
        GuestId = secondUserId;
    }

    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        var battleStarts = (userId == HostId && guestFleet is not null) ||
            (userId == GuestId && HostFleet is not null);
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.x, deckLocation.y))
                .ToDictionary(x => x.Location)
        }).ToArray();
        UpdateState(userId, newShips);
        if(battleStarts) RenewTurnTimer();
    }

    private void UpdateState(int userId, Ship[] newShips)
    {
        if (userId == HostId)
        {
            hostFleet = newShips;
            State = guestFleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
        }
        else
        {
            guestFleet = newShips;
            State = hostFleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
        }
    }

    //todo tdd userId field
#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public AttackResult Attack(int userId, Cell attackedLocation)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
    {
        AssertThatShotIsInFieldBorders(attackedLocation);
        //todo tdd that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo check for 3 times
        var player1Turn = State == GameState.HostTurn;
        var attackedShips = player1Turn ?
            guestFleet!.Where(x => !IsDestroyed(x)).ToArray() : hostFleet!.Where(x => !IsDestroyed(x)).ToArray();
        var result = AttackResult.Missed;
        ProcessHit(attackedLocation, GetAttackedShip(attackedLocation, attackedShips), ref result);
        ProcessBattleOrWin(player1Turn, attackedShips, ref result);
        if (this.BattleOngoing) RenewTurnTimer();
        return result; //todo tdd correct result
    }

    protected virtual void RenewTurnTimer(int secondsLeft = 30) => RenewTimerInternal(secondsLeft);

    //todo mb I shouldn't call it from test setups
    protected void RenewTimerInternal(int secondsLeft = 30)
    {
        turnTimer?.Dispose();
        turnTimer = new TimerPlus(state => State = WhoWillWinWhenTurnTimeEnds(), this,
            TimeSpan.FromSeconds(secondsLeft), Timeout.InfiniteTimeSpan);
    }

    protected GameState WhoWillWinWhenTurnTimeEnds() =>
        State == GameState.HostTurn ? GameState.GuestWon : GameState.HostWon;

    private void ProcessBattleOrWin(bool player1Turn, IEnumerable<Ship> attackedShips,
        ref AttackResult result)
    {
        if (attackedShips.All(x => IsDestroyed(x)))
        {
            State = player1Turn ? GameState.HostWon : GameState.GuestWon;
            result = AttackResult.Win;
            DisposeOfTimer();
        }
        else
        {
            var hit = result == AttackResult.Hit;
            if(player1Turn && hit || !player1Turn && !hit) State = GameState.HostTurn;
            if(!player1Turn && hit || player1Turn && !hit) State = GameState.GuestTurn;
        }
    }

    private static void ProcessHit(Cell attackedLocation, Ship? attackedShip, ref AttackResult result)
    {
        //todo tdd throw if null?
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            result = AttackResult.Hit;
        }
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship =>
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private void Exclude(Cell location)
    {
        //todo check for 3 times
        var currentExcluded = State == GameState.HostTurn ? hostExcludedLocations : guestExcludedLocations;
        if (currentExcluded.Contains(location))
            throw new Exception($"Location {location} is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<Cell> hostExcludedLocations = new();
    protected List<Cell> guestExcludedLocations = new();
    //todo tdd validate ship shape
    protected Ship[]? hostFleet;
    protected Ship[]? guestFleet;
    protected TimerPlus? turnTimer;
    private GameState state;

    //todo to Ship extension!!! 
    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);

    private static void AssertThatShotIsInFieldBorders(Cell attackedLocation)
    {
        if (attackedLocation.x < 0 || attackedLocation.x > 9 ||
                    attackedLocation.y < 0 || attackedLocation.y > 9)
            throw new Exception(
                "Target cannot be outside the game field. Available coordinates are 0-9.");
    }
}