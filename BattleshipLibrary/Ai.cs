namespace BattleshipLibrary;

public class Ai : IAi
{
    //todo test
    public Cell ChooseAttackLocation(IEnumerable<Ship> enemyShips, IEnumerable<Cell> excludedLocations)
    {
        var shotDeckLocations =
            enemyShips.SelectMany(x => x.Decks.Values).Where(x => x.Destroyed).Select(x => x.Location);
        var unavailableLocations = shotDeckLocations.Concat(excludedLocations);
        var allLocations = from xCoord in Enumerable.Range(0, 10)
            join yCoord in Enumerable.Range(0, 10) on true equals true
            select new Cell(xCoord, yCoord);
        var availableLocations = allLocations.Except(unavailableLocations).ToArray();
        var choiceIndex = random.Next(availableLocations.Length);
        var choice = availableLocations[choiceIndex];
        return choice;
    }

    //todo test
    public Ship[] GenerateShips()
    {
        var result = new List<Ship>();
        result.Add(GenerateShip(4, result));
        result.Add(GenerateShip(3, result));
        result.Add(GenerateShip(3, result));
        for (int i = 0; i < 3; i++) result.Add(GenerateShip(2, result));
        return result.ToArray();
    }

    private Ship GenerateShip(int decksCount, List<Ship> existingShips)
    {
        List<Deck> decks;
        do
        {
            decks = new List<Deck>();
            var initialLocation = new Cell(random.Next(10), random.Next(10));
            var availableDirections = GetAvailableDirections(initialLocation, decksCount);
            var choice = availableDirections[random.Next(availableDirections.Count)];
            for (int i = 0; i < decksCount; i++)
            {
                var dx = GetDx(choice, i);
                var dy = GetDy(choice, i);
                decks.Add(new Deck(initialLocation.X + dx, initialLocation.Y + dy));
            }
        } while (decks.Any(deck => DeckHasConflicts(existingShips, deck)));

        var ship = new Ship { Decks = decks.ToDictionary(x => x.Location) };
        return ship;
    }

    private readonly Random random = new();

    private static bool DeckHasConflicts(List<Ship> existingShips, Deck deck)
    {
        var existingDeckLocations = existingShips.SelectMany(ship => ship.Decks.Keys);
        var withSurrounding =
            existingDeckLocations.Select(GetSurroundingCells).SelectMany(x => x).Distinct();
        return withSurrounding.Contains(deck.Location);
    }

    private static Cell[] GetSurroundingCells(Cell cell) =>
        new[]
        {
            new Cell(cell.X, cell.Y + 1),
            new Cell(cell.X + 1, cell.Y + 1),
            new Cell(cell.X + 1, cell.Y),
            new Cell(cell.X + 1, cell.Y - 1),
            new Cell(cell.X, cell.Y - 1),
            new Cell(cell.X - 1, cell.Y - 1),
            new Cell(cell.X - 1, cell.Y),
            new Cell(cell.X - 1, cell.Y + 1)
        };

    private static List<Direction> GetAvailableDirections(Cell initialLocation, int decksCount)
    {
        var result = new List<Direction>();
        var upperBound = 9 - decksCount + 1;
        var lowerBound = decksCount - 1;
        if (initialLocation.Y <= upperBound) result.Add(Direction.Up);
        if (initialLocation.X <= upperBound) result.Add(Direction.Right);
        if (initialLocation.Y >= lowerBound) result.Add(Direction.Down);
        if (initialLocation.X >= lowerBound) result.Add(Direction.Left);
        return result;
    }

    private static int GetDy(Direction choice, int i) =>
        choice switch
        {
            Direction.Up => i,
            Direction.Right => 0,
            Direction.Down => -i,
            Direction.Left => 0,
            _ => throw new Exception($"Unknown direction: [{choice}].")
        };

    private static int GetDx(Direction choice, int i) =>
        choice switch
        {
            Direction.Up => 0,
            Direction.Right => i,
            Direction.Down => 0,
            Direction.Left => -i,
            _ => throw new Exception($"Unknown direction: [{choice}].")
        };

    private enum Direction
    {
        Up,
        Right,
        Left,
        Down
    }
}