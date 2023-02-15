﻿namespace BattleshipLibrary;

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
        FirstUserId ??= userId;
        var newShips = ships.Select(ship => new Ship
        {
            Decks = ship.Decks.Keys.Select(deckLocation => new Deck(deckLocation.x, deckLocation.y))
                .ToDictionary(x => x.Location)
        }).ToArray();
        UpdateState(userId, newShips);
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
        //todo tdd that we can't get here with playerNShips == null
        Exclude(attackedLocation);
        //todo tdd this condition
        //todo check for 3 times
        var player1Turn = State == GameState.Player1Turn;
        var attackedShips = player1Turn ?
            secondFleet!.Where(x => !IsDestroyed(x)) : firstFleet!.Where(x => !IsDestroyed(x));
        var result = AttackResult.Missed;
        ProcessHit(attackedLocation, GetAttackedShip(attackedLocation, attackedShips), ref result);
        SetStateForBattleOrWin(player1Turn, attackedShips, ref result);
        return result; //todo tdd correct result
    }

    private void SetStateForBattleOrWin(bool player1Turn, IEnumerable<Ship> attackedShips, 
        ref AttackResult result)
    {
        if (attackedShips.All(x => IsDestroyed(x)))
        {
            State = player1Turn ? GameState.Player1Won : GameState.Player2Won;
            result = AttackResult.Win;
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

    public static bool IsDestroyed(Ship ship) => ship.Decks.Values.All(x => x.Destroyed);
}