using NLog;
using System.Timers;

namespace BattleshipLibrary;

public class Game
{
    public Game(int user1Id, IAi ai, int matchingTimeSeconds = 30)
    {
        Host = new User { Id = user1Id };
        Id = new Random().Next();
        SetMatchingTimer(matchingTimeSeconds);
        this.ai = ai;
    }

    public User Host { get; set; }
    public User? Guest { get; set; }

    //todo tdd this field
    public int Id { get; private set; }

    //todo test
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

    //todo test
    public int? TimerSecondsLeft =>
        timer is null ? null : (int)Math.Ceiling(timer.DueTime.TotalMilliseconds / 1000f);

    //todo test
    public bool BattleOngoing => State == GameState.HostTurn || State == GameState.GuestTurn;

    public bool CreatingFleets =>
        State == GameState.BothPlayersCreateFleets || State == GameState.OnePlayerCreatesFleet;

    public bool ItsOver => State == GameState.HostWon || State == GameState.GuestWon ||
        State == GameState.Cancelled;

    //todo tdd
    public void DisposeOfTimer()
    {
        timer?.Dispose();
        timer = null;
    }

    //todo test
    public void SetTechnicalWinner(bool hostWon)
    {
        DisposeOfTimer();
        State = hostWon ? GameState.HostWon : GameState.GuestWon;
    }

    //todo test
    public void Start(int guestId)
    {
        State = GameState.BothPlayersCreateFleets;
        Guest = new User { Id = guestId };
        SetShipsCreationTimer(60);
    }

    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        var battleStarts = (userId == Host!.Id && Guest!.Fleet is not null) ||
            (userId == Guest!.Id && Host.Fleet is not null);
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.x, deckLocation.y))
                .ToDictionary(x => x.Location)
        }).ToArray();
        UpdateState(userId, newShips);
        if(battleStarts) SetBattleTimer();
    }

    private void UpdateState(int userId, Ship[] newShips)
    {
        if (userId == Host.Id)
        {
            Host!.Fleet = newShips;
            State = Guest!.Fleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
        }
        else
        {
            Guest!.Fleet = newShips;
            State = Host!.Fleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
        }
    }

    //todo refactor long method
    //todo tdd userId field
#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public AttackResult Attack(int userId, Cell attackedLocation)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
    {
        AssertThatShotIsInFieldBorders(attackedLocation);
        //todo test that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo check for 3 times
        var player1Turn = State == GameState.HostTurn;
        var attackedShips = player1Turn 
            ? Guest!.Fleet!.Where(x => !x.IsDestroyed).ToArray() 
            : Host!.Fleet!.Where(x => !x.IsDestroyed).ToArray();
        var result = AttackResult.Missed;
        var attackedShip = GetAttackedShip(attackedLocation, attackedShips);
        //todo tdd throw if null?
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            result = AttackResult.Hit;
        }
        if (attackedShips.All(x => x.IsDestroyed))
        {
            State = player1Turn ? GameState.HostWon : GameState.GuestWon;
            result = AttackResult.Win;
            DisposeOfTimer();
        }
        else
        {
            var hit = result == AttackResult.Hit;
            if (player1Turn && hit || !player1Turn && !hit) State = GameState.HostTurn;
            if (!player1Turn && hit || player1Turn && !hit) State = GameState.GuestTurn;
        }
        if (BattleOngoing)
        {
            if (Guest!.IsBot)
            {
                var aiAttackLocation = ai.ChooseAttackLocation();
                Exclude(aiAttackLocation);
                attackedShips = Host.Fleet!;
                attackedShip =
                    attackedShips.SingleOrDefault(x => x.Decks.ContainsKey(aiAttackLocation));
                if(attackedShip is not null) attackedShip.Decks[aiAttackLocation].Destroyed = true;
                if (attackedShips.All(x => x.IsDestroyed))
                {
                    State = GameState.GuestWon;
                    result = AttackResult.Win;
                    DisposeOfTimer();
                }
                else State = GameState.HostTurn;
            } 
            SetBattleTimer();
        }
        return result; //todo tdd correct result
    }

    protected virtual void SetMatchingTimer(int secondsLeft = 30) =>
        SetTimerWithAction(() =>
        {
            State = GameState.OnePlayerCreatesFleet;
            Guest = new User { IsBot = true, Fleet = ai.GenerateShips(), Name = "General Chaos" };
        }, secondsLeft);

    protected virtual void SetShipsCreationTimer(int secondsLeft = 30) =>
        SetTimerWithAction(() =>
        {
            if (Host.Fleet is not null && Guest!.Fleet is null) SetTechnicalWinner(true);
            else if (Host.Fleet is null && Guest!.Fleet is not null) SetTechnicalWinner(false);
            else if (Host.Fleet is null && Guest!.Fleet is null)
            {
                DisposeOfTimer();
                State = GameState.Cancelled;
            }
        }, secondsLeft);

    protected virtual void SetBattleTimer(int secondsLeft = 30) => 
        SetTimerWithAction(() => 
            State = State == GameState.HostTurn ? GameState.GuestWon : GameState.HostWon, 
            secondsLeft);

    protected TimerWithDueTime? timer;
    protected readonly IAi ai;

    private void SetTimerWithAction(Action action, int secondsLeft)
    {
        timer?.Dispose();
        timer = new TimerWithDueTime(action, TimeSpan.FromSeconds(secondsLeft));
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship =>
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private void Exclude(Cell location)
    {
        //todo check for 3 times
        var currentExcluded = 
            State == GameState.HostTurn ? Host!.ExcludedLocations : Guest!.ExcludedLocations;
        if (currentExcluded.Contains(location))
            throw new Exception($"Location {location} is already excluded.");
        currentExcluded.Add(location);
    }

    private GameState state;

    private static void AssertThatShotIsInFieldBorders(Cell attackedLocation)
    {
        if (attackedLocation.x < 0 || attackedLocation.x > 9 ||
            attackedLocation.y < 0 || attackedLocation.y > 9)
            throw new Exception(
                "Target cannot be outside the game field. Available coordinates are 0-9.");
    }
}