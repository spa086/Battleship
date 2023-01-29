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

    public int? FirstUserId { get; protected set; }
    public int? SecondUserId { get; protected set; }

    public GameState State { get; protected set; }

    public List<Cell> ExcludedLocations1 => excludedLocations1;
    public List<Cell> ExcludedLocations2 => excludedLocations2;
    public bool Win => win;
    public List<Ship>? FirstFleet => firstFleet;
    public List<Ship>? SecondFleet => secondFleet;

    //todo test
    public void Start(int secondUserId)
    {
        State = GameState.BothPlayersCreateFleets;
        SecondUserId = secondUserId;
    } 


    public void CreateAndSaveShips(int userId, IEnumerable<Ship> ships)
    {
        if(FirstUserId is null) FirstUserId = userId;
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.x, deckLocation.y))
                .ToDictionary(x => x.Location)
        }).ToList();
        UpdateState(userId, newShips);
    }

    private void UpdateState(int userId, List<Ship> newShips)
    {
        if (userId == FirstUserId)
        {
            firstFleet = newShips;
            State = GameState.WaitingForPlayer2ToCreateFleet;
        }
        else
        {
            secondFleet = newShips;
            State = GameState.Player1Turn;
        }
    }

    //todo tdd userId field
#pragma warning disable IDE0060 // Удалите неиспользуемый параметр
    public AttackResult Attack(int userId, Cell attackedLocation)
#pragma warning restore IDE0060 // Удалите неиспользуемый параметр
    {
        //todo tdd that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo tdd this condition
        //todo check for 3 times
        var player1Turn = State == GameState.Player1Turn;
        var attackedShips = player1Turn ?
            secondFleet!.Where(x => !IsDestroyed(x)) : firstFleet!.Where(x => !IsDestroyed(x));
        var result = AttackResult.Missed;
        ProcessHit(attackedLocation, GetAttackedShip(attackedLocation, attackedShips), ref result);
        ProcessWin(player1Turn, attackedShips, ref result);
        return result; //todo tdd correct result
    }

    private void ProcessWin(bool player1Turn, IEnumerable<Ship> attackedShips, ref AttackResult result)
    {
        if (attackedShips.All(x => IsDestroyed(x)))
        {
            win = true;
            result = AttackResult.Win;
        }
        else State = player1Turn ? GameState.Player2Turn : GameState.Player1Turn; //todo tdd this
    }

    private static void ProcessHit(Cell attackedLocation, Ship? attackedShip, ref AttackResult result)
    {
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
            throw new Exception($"Location [{location}] is already excluded.");
        currentExcluded.Add(location);
    }

    protected List<Cell> excludedLocations1 = new();
    protected List<Cell> excludedLocations2 = new();
    //todo tdd validate ship shape
    protected List<Ship>? firstFleet;
    protected List<Ship>? secondFleet;
    protected bool win;

    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);
}

