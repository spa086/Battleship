namespace BattleshipLibrary;

public class Game
{
    public Game(int user1Id, IAi ai, int matchingTimeSeconds = 30)
    {
        Host = new User { Id = user1Id };
        Id = new Random().Next();
        // ReSharper disable once VirtualMemberCallInConstructor
        SetMatchingTimer(matchingTimeSeconds);
        Ai = ai;
        StartTime = DateTime.Now;
    }

    public DateTime StartTime { get; }

    public User Host { get; }
    public User? Guest { get; set; }

    public int Id { get; }

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

    public int? TimerSecondsLeft =>
        TheTimer is null ? null : (int)Math.Ceiling(TheTimer.DueTime.TotalMilliseconds / 1000f);

    public bool BattleOngoing => State == GameState.HostTurn || State == GameState.GuestTurn;

    public bool CreatingFleets =>
        State == GameState.BothPlayersCreateFleets || State == GameState.OnePlayerCreatesFleet;

    public bool ItsOver => State == GameState.HostWon || State == GameState.GuestWon ||
                           State == GameState.Cancelled;

    public void Cancel() => State = GameState.Cancelled;

    public void DisposeOfTimer()
    {
        TheTimer?.Dispose();
        TheTimer = null;
    }

    public void SetTechnicalWinner(bool hostWon)
    {
        DisposeOfTimer();
        State = hostWon ? GameState.HostWon : GameState.GuestWon;
    }

    public void Start(int guestId)
    {
        State = GameState.BothPlayersCreateFleets;
        Guest = new User { Id = guestId };
        SetShipsCreationTimer(60);
    }

    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        var battleStarts = (userId == Host.Id && Guest!.Fleet is not null) ||
                           (userId == Guest!.Id && Host.Fleet is not null);
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.X, deckLocation.Y))
                .ToDictionary(x => x.Location)
        }).ToArray();
        UpdateState(userId, newShips);
        if (battleStarts) SetBattleTimer();
    }

#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public AttackResult Attack(int userId, Cell attackedLocation)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
    {
        AssertThatShotIsInFieldBorders(attackedLocation);
        var result = PerformAttackByUser(attackedLocation);
        if (!BattleOngoing) return result; //todo tdd correct result
        if (Guest!.IsBot && result != AttackResult.Hit) AiTurn();
        SetBattleTimer();
        return result; //todo tdd correct result
    }

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
            State = State == GameState.HostTurn ? GameState.GuestWon : GameState.HostWon, secondsLeft);

    protected TimerWithDueTime? TheTimer;
    protected readonly IAi Ai;

    private AttackResult PerformAttackByUser(Cell attackedLocation)
    {
        //todo test that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo check for 3 times
        var player1Turn = State == GameState.HostTurn;
        var attackedShips = player1Turn
            ? Guest!.Fleet!.Where(x => !x.IsDestroyed).ToArray()
            : Host.Fleet!.Where(x => !x.IsDestroyed).ToArray();
        var result = AttackResult.Missed;
        if (AttackDeck(attackedLocation, attackedShips)) result = AttackResult.Hit;
        if (attackedShips.All(x => x.IsDestroyed)) EndGameWithVictory(player1Turn);
        else PassTurn(player1Turn, result);
        if (ItsOver) result = AttackResult.Win;
        return result;
    }

    private void UpdateState(int userId, Ship[] newShips)
    {
        if (userId == Host.Id)
        {
            Host.Fleet = newShips;
            State = Guest!.Fleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
        }
        else
        {
            Guest!.Fleet = newShips;
            State = Host.Fleet is not null ? GameState.HostTurn : GameState.OnePlayerCreatesFleet;
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

    private bool PerformAiAttack()
    {
        var aiAttackLocation = Ai.ChooseAttackLocation(Host.Fleet!, Guest!.ExcludedLocations);
        Exclude(aiAttackLocation);
        var aiAttackedShips = Host.Fleet!;
        var aiAttackedShip = GetAttackedShip(aiAttackLocation, aiAttackedShips);
        if (aiAttackedShip is not null) aiAttackedShip.Decks[aiAttackLocation].Destroyed = true;
        if (aiAttackedShips.Any(x => !x.IsDestroyed)) return aiAttackedShip is not null;
        State = GameState.GuestWon;
        DisposeOfTimer();

        return aiAttackedShip is not null;
    }

    private void SetTimerWithAction(Action action, int secondsLeft)
    {
        TheTimer?.Dispose();
        TheTimer = new TimerWithDueTime(action, TimeSpan.FromSeconds(secondsLeft));
    }

    private void Exclude(Cell location)
    {
        //todo check for 3 times
        var currentExcluded = State == GameState.HostTurn ? Host.ExcludedLocations : Guest!.ExcludedLocations;
        if (currentExcluded.Contains(location)) 
            throw new Exception($"Location {location} is already excluded.");
        currentExcluded.Add(location);
    }
    
    private void SetMatchingTimer(int secondsLeft = 30) =>
        SetTimerWithAction(() =>
        {
            State = GameState.OnePlayerCreatesFleet;
            Guest = new User { IsBot = true, Fleet = Ai.GenerateShips(), Name = "General Chaos" };
            SetShipsCreationTimer(60);
        }, secondsLeft);

    private GameState state;

    private static bool AttackDeck(Cell attackedLocation, Ship[] attackedShips)
    {
        var attackedShip = GetAttackedShip(attackedLocation, attackedShips);
        //todo tdd throw if null?
        if (attackedShip is null) return false;
        attackedShip.Decks.Values.Single(x => x.Location == attackedLocation).Destroyed = true;
        return true;
    }

    private static Ship? GetAttackedShip(Cell attackedLocation, IEnumerable<Ship> attackedShips) =>
        attackedShips.SingleOrDefault(ship => ship.Decks.Values.Any(deck => deck.Location == attackedLocation));

    private static void AssertThatShotIsInFieldBorders(Cell attackedLocation)
    {
        if (attackedLocation.X < 0 || attackedLocation.X > 9 ||
            attackedLocation.Y < 0 || attackedLocation.Y > 9)
            throw new Exception(
                "Target cannot be outside the game field. Available coordinates are 0-9.");
    }

    private void AiTurn()
    {
        State = GameState.GuestTurn;
        while (PerformAiAttack() && BattleOngoing)
        {
        }

        if (BattleOngoing) State = GameState.HostTurn;
    }
}