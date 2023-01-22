using BattleshipApi;
using BattleshipLibrary;

var controller = new Controller();
do
{
    PrepareConsole();
    ConsoleKey? choice = null;
    do
    {
        choice = Console.ReadKey().Key;
        if (choice == ConsoleKey.G) ProcessCreateGameDecision();
        else if (choice == ConsoleKey.J) ProcessJoinGameDecision();
        else if (choice == ConsoleKey.S) ProcessCreateShipsDecision();
        else if (choice == ConsoleKey.A) ProcessAttackDecision();
    } while (choice != ConsoleKey.G && choice != ConsoleKey.J && 
        choice != ConsoleKey.S && choice != ConsoleKey.A);
} while (true);

Cell ReadCell()
{
    var cellStr = Console.ReadLine()!;
    var parts = cellStr.Split(',');
    var x = int.Parse(parts[0]);
    var y = int.Parse(parts[1]);
    var result = new Cell(x, y);
    return result;
}

void ProcessAttackDecision()
{
    Console.WriteLine("Please enter your user id: ");
    var userId = int.Parse(Console.ReadLine()!);
    Console.WriteLine("Where to attack? Enter location: ");
    var attackocation = ReadCell();
    var result = controller.Attack(
        new AttackRequestModel 
        { 
            location = new LocationModel { x = attackocation.x, y = attackocation.y } 
        });
    Console.WriteLine($"User id=[{userId}] has attacked location [{attackocation}]. Result: [{result.result}].");
}

string GetBoolString(bool isDestroyed) => isDestroyed ? "t" : "F";

string GetFleetString(IEnumerable<Ship> fleet) => 
    string.Join("; ", fleet.Select(ship =>
    {
        var shipStr = string.Join(", ",
            ship.Decks.Select(deck => $"{deck.Key}{GetBoolString(deck.Value.Destroyed)}"));
        return $"[{shipStr}]";
    }));

void PrepareConsole()
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
    Console.WriteLine($"Welcome to BATTLESHIPS by FIERCE DEVELOPMENT!");
    ShowCurrentGameIfExists();
    var optionsDescription = "Do you wish to create a game (g), join existing one (j)";
    var game = GamePool.TheGame;
    if (game is not null)
    {
        if (game.State == GameState.BothPlayersCreateFleets ||
                game.State == GameState.WaitingForPlayer2ToCreateFleet)
            optionsDescription += " or create ships (s)";
        if (game.State == GameState.Player1Turn || game.State == GameState.Player2Turn)
            optionsDescription += " or attack (a)";
    }
    optionsDescription += "?";
    Console.WriteLine(optionsDescription);
}

void ShowCurrentGameIfExists()
{
    var game = GamePool.TheGame;
    if(game is null) return;
    if(game.FirstUserId is not null)
    {
        Console.WriteLine($"First user id = [{game.FirstUserId}].");
    }
    if (game.SecondUserId is not null)
    {
        Console.WriteLine($"Second user id = [{game.SecondUserId}].");
    }
    if (game.FirstFleet is not null)
    {
        var player1FleetStr = GetFleetString(game!.FirstFleet!);
        //todo check 3 times
        Console.WriteLine($"Player 1 ships: {{{player1FleetStr}}}.");
    }
    if (game.SecondFleet is not null)
    {
        var player2FleetStr = GetFleetString(game!.SecondFleet!);
        //todo check 3 times
        Console.WriteLine($"Player 2 ships: {{{player2FleetStr}}}.");
    }
}

void ProcessCreateShipsDecision()
{
    Console.WriteLine("Enter your user id: ");
    var idStr = Console.ReadLine();
    var id = int.Parse(idStr!);
    Console.WriteLine("Enter ships in format \"[d1;d2;..]-[e1;e2;..];..\" " +
                "where di, ei are deck coordinates di=xi,yi.");
    var decksArrays = Console.ReadLine()!.Split("-")
        .Select(x => x[1..^1]).ToArray()
        .Select(shipString =>
        {
            var decksStrings = shipString.Split(';');
            var decks = decksStrings.Select(deckString =>
            {
                var parts = deckString.Split(',');
                return new LocationModel
                {
                    x = int.Parse(parts[0]),
                    y = int.Parse(parts[1])
                };
            }).ToArray();
            return new ShipTransportModel { decks = decks };
        }).ToArray();
    var response = controller.CreateFleet(new FleetCreationRequestModel
    { userId = id, ships = decksArrays });
    Console.WriteLine($"User id=[{id}] has created ships. Response: [{response}].");
}

void ProcessJoinGameDecision()
{
    var userId = new Random().Next(100);
    var response = controller.WhatsUp(new WhatsupRequestModel { userId = userId });
    var game = GamePool.TheGame!;
    Console.WriteLine($"User id=[{userId}] has joined game created by user id=[{game.FirstUserId}]. " +
        $"Response: [{response}].");
}

void ProcessCreateGameDecision()
{
    var userId = new Random().Next(100);
    var response = controller.WhatsUp(new WhatsupRequestModel { userId = userId });
    Console.WriteLine($"User id=[{userId}] has created game. Response: [{response}].");
}