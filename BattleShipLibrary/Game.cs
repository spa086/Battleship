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

    //todo tdd userId field
#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public AttackResult Attack(int userId, Cell attackedLocation)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
    {
        AssertThatShotIsInFieldBorders(attackedLocation);
        var result = PerformAttackByUser(attackedLocation);
        if (BattleOngoing)
        {
            if (Guest!.IsBot) PerformAiAttack();
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

    private AttackResult PerformAttackByUser(Cell attackedLocation)
    {
        //todo test that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo check for 3 times
        var player1Turn = State == GameState.HostTurn;
        var attackedShips = player1Turn
            ? Guest!.Fleet!.Where(x => !x.IsDestroyed).ToArray()
            : Host!.Fleet!.Where(x => !x.IsDestroyed).ToArray();
        var result = AttackResult.Missed;
        if (AttackDeck(attackedLocation, attackedShips))
            result = AttackResult.Hit;
        if (attackedShips.All(x => x.IsDestroyed)) EndGameWithVictory(player1Turn);
        else PassTurn(player1Turn, result);
        if (ItsOver) result = AttackResult.Win;
        return result;
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

    private void EndGameWithVictory(bool player1Turn)
    {
        State = player1Turn ? GameState.HostWon : GameState.GuestWon;
        DisposeOfTimer();
    }

    private void PassTurn(bool player1Turn, AttackResult result)
    {
        var hit = result == AttackResult.Hit;
        if (player1Turn && hit || !player1Turn && !hit) State = GameState.HostTurn;
        if (!player1Turn && hit || player1Turn && !hit) State = GameState.GuestTurn;
    }

    private void PerformAiAttack()
    {
        var aiAttackLocation = ai.ChooseAttackLocation(Host.Fleet!, Guest!.ExcludedLocations);
        Exclude(aiAttackLocation);
        var aiAttackedShips = Host.Fleet!;
        var aiAttackedShip = GetAttackedShip(aiAttackLocation, aiAttackedShips);
        if (aiAttackedShip is not null) aiAttackedShip.Decks[aiAttackLocation].Destroyed = true;
        if (aiAttackedShips.All(x => x.IsDestroyed))
        {
            State = GameState.GuestWon;
            DisposeOfTimer();
        }
        else State = GameState.HostTurn;
    }

    private void SetTimerWithAction(Action action, int secondsLeft)
    {
        timer?.Dispose();
        timer = new TimerWithDueTime(action, TimeSpan.FromSeconds(secondsLeft));
    }

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

    private static bool AttackDeck(Cell attackedLocation, Ship[] attackedShips)
    {
        var attackedShip = GetAttackedShip(attackedLocation, attackedShips);
        //todo tdd throw if null?
        if (attackedShip is not null)
        {
            attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
            return true;
        }
        return false;
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        //todo tdd this condition
        attackedShips.SingleOrDefault(ship =>
            ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private static void AssertThatShotIsInFieldBorders(Cell attackedLocation)
    {
        if (attackedLocation.x < 0 || attackedLocation.x > 9 ||
            attackedLocation.y < 0 || attackedLocation.y > 9)
            throw new Exception(
                "Target cannot be outside the game field. Available coordinates are 0-9.");
    }
}