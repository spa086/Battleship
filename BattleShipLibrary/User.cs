namespace BattleshipLibrary;

public class User
{
    public bool IsBot { get; init; }
    public int Id { get; set; }

    public string? Name { get; set; }

    public List<Cell> ExcludedLocations { get; set; } = new();

    //todo tdd validate ship shape
    public Ship[]? Fleet { get; set; }
}
