using System.Timers;

namespace BattleshipLibrary;

public class Game
{
    public Game(int user1Id)
    {
        FirstUserId = user1Id;
        var random = new Random();
        Id = random.Next();
    }

    //todo tdd this field
    public int Id { get; private set; }

    //todo make User class
    public int FirstUserId { get; protected set; }
    //todo persist user name between games
    public string? FirstUserName { get; set; }
    public int? SecondUserId { get; protected set; }
    //todo tdd this field in whatsup tests
    public string? SecondUserName { get; set; }

    public GameState State { get; protected set; }
    public int? TurnSecondsLeft => 
        turnTimer is null ? null : (int)Math.Ceiling(turnTimer.DueTime.TotalMilliseconds/1000f);

    public List<Cell> ExcludedLocations1 => excludedLocations1;
    public List<Cell> ExcludedLocations2 => excludedLocations2;
    public Ship[]? FirstFleet => firstFleet;
    public Ship[]? SecondFleet => secondFleet;

    public bool BattleOngoing => State == GameState.Player1Turn || State == GameState.Player2Turn;
    public bool CreatingFleets => 
        State == GameState.BothPlayersCreateFleets || State == GameState.OnePlayerCreatesFleet;
    public bool ItsOver => State == GameState.Player1Won || State == GameState.Player2Won;

    //todo test
    public void Start(int secondUserId)
    {
        State = GameState.BothPlayersCreateFleets;
        SecondUserId = secondUserId;
    } 

    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        var battleStarts = (userId == FirstUserId && SecondFleet is not null) ||
            (userId == SecondUserId && FirstFleet is not null);
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
        if (userId == FirstUserId)
        {
            firstFleet = newShips;
            State = secondFleet is not null ? GameState.Player1Turn : GameState.OnePlayerCreatesFleet;
        }
        else
        {
            secondFleet = newShips;
            State = firstFleet is not null ? GameState.Player1Turn : GameState.OnePlayerCreatesFleet;
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
        var player1Turn = State == GameState.Player1Turn;
        var attackedShips = player1Turn ?
            secondFleet!.Where(x => !IsDestroyed(x)) : firstFleet!.Where(x => !IsDestroyed(x));
        var result = AttackResult.Missed;
        ProcessHit(attackedLocation, GetAttackedShip(attackedLocation, attackedShips), ref result);
        ProcessBattleOrWin(player1Turn, attackedShips, ref result);
        if (this.BattleOngoing) RenewTurnTimer();
        return result; //todo tdd correct result
    }

    protected virtual void RenewTurnTimer(int secondsLeft = 30) => RenewTimerInternal(secondsLeft);

    protected void RenewTimerInternal(int secondsLeft = 30)
    {
        turnTimer?.Dispose();
        turnTimer = new TimerPlus(state => State = WhoWillWinWhenTurnTimeEnds(), this,
            TimeSpan.FromSeconds(secondsLeft), Timeout.InfiniteTimeSpan);
    }

    protected GameState WhoWillWinWhenTurnTimeEnds() => 
        State == GameState.Player1Turn ? GameState.Player2Won : GameState.Player1Won;

    private void ProcessBattleOrWin(bool player1Turn, IEnumerable<Ship> attackedShips, 
        ref AttackResult result)
    {
        if (attackedShips.All(x => IsDestroyed(x)))
        {
            State = player1Turn ? GameState.Player1Won : GameState.Player2Won;
            result = AttackResult.Win;
            turnTimer?.Dispose();
            turnTimer = null;
        }
        else
        {
            var hit = result == AttackResult.Hit;
            if(player1Turn && hit || !player1Turn && !hit) State = GameState.Player1Turn;
            if(!player1Turn && hit || player1Turn && !hit) State = GameState.Player2Turn;
        }
    }

    private static void ProcessHit(Cell attackedLocation, Ship? attackedShip, ref AttackResult result)
    {
        //todo tdd throw if null?
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            if (attackedShip.Decks.All(x => x.Value.Destroyed)) result = AttackResult.Killed;
            else result = AttackResult.Hit;
        }
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship => 
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private void Exclude(Cell location)
    {
        //todo check for 3 times
        var currentExcluded = State == GameState.Player1Turn ? excludedLocations1 : excludedLocations2;
        if (currentExcluded.Contains(location)) 
            throw new Exception($"Location {location} is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<Cell> excludedLocations1 = new();
    protected List<Cell> excludedLocations2 = new();
    //todo tdd validate ship shape
    protected Ship[]? firstFleet;
    protected Ship[]? secondFleet;
    protected TimerPlus? turnTimer;

    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);

    private static void AssertThatShotIsInFieldBorders(Cell attackedLocation)
    {
        if (attackedLocation.x < 0 || attackedLocation.x > 9 ||
                    attackedLocation.y < 0 || attackedLocation.y > 9)
            throw new Exception(
                "Target cannot be outside the game field. Available coordinates are 0-9.");
    }
}

public class TimerPlus : IDisposable
{
    public TimerPlus(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        timer = new System.Threading.Timer(Callback, state, dueTime, period);
        realCallback = callback;
        this.period = period;
        next = DateTime.Now.Add(dueTime);
    }

    public TimeSpan DueTime => next - DateTime.Now;

    public void Dispose()
    {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Callback(object? state)
    {
        next = DateTime.Now.Add(period);
        realCallback(state);
    }

    private readonly TimerCallback realCallback;
    private readonly System.Threading.Timer timer;
    private readonly TimeSpan period;
    private DateTime next;
}