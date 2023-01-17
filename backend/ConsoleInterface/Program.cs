using BattleshipApi;
using BattleShipLibrary;

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
    } while (choice != ConsoleKey.G && choice != ConsoleKey.J && choice != ConsoleKey.S);
} while (true);

void ProcessJoinGameDecision()
{
    Console.WriteLine("Which one? Enter id: ");
    var idStr = Console.ReadLine();
    var id = int.Parse(idStr!);
    var response = controller.WhatsUp(new WhatsupRequestModel { SessionId = id });
    Console.WriteLine($"Joined game with id = [{id}]. Response: [{response}].");
}

void PrepareConsole()
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
    Console.WriteLine($"Welcome to BATTLESHIPS by FIERCE DEVELOPMENT!");
    var sessionModels = GamePool.Games.Select(x => new { sessionId = x.Key, x.Value.State });
    var sessionInfoStrings = sessionModels.Select(x => $"[id={x.sessionId};state={x.State}]");
    if (sessionInfoStrings.Any())
        Console.WriteLine($"Ongoing sessions: [{string.Join(", ", sessionInfoStrings)}].");
    var optionsDescription = "Do you wish to create a game (g), join existing one (j)";
    if(sessionModels.Any(x => x.State == GameState.BothPlayersCreateFleets ))
        optionsDescription += " or create ships (s)";
    optionsDescription += "?";
    Console.WriteLine(optionsDescription);
}

void ProcessCreateShipsDecision()
{
    Console.WriteLine("Which one? Enter id: ");
    var idStr = Console.ReadLine();
    var id = int.Parse(idStr!);
    Console.WriteLine("Enter ships in format \"[d1,d2,..];[e1,e2,..];..\" " +
                "where di, ei etc. are deck coordinates...");
    var decksArrays = Console.ReadLine()!.Split(",")
        .Select(x => x[1..^1]).ToArray()
        .Select(shipString =>
        {
            var decksStrings = shipString.Split(',');
            var decks = decksStrings.Select(deckString => int.Parse(deckString)).ToArray();
            return new ShipTransportModel { Decks = decks };
        }).ToArray();
    var response = controller.CreateFleet(new FleetCreationRequestModel
        { SessionId = id, Ships = decksArrays });
    Console.WriteLine($"Ships created with game id = [{id}]. Response: [{response}].");
}

void ProcessCreateGameDecision()
{
    var sessionId = new Random().Next(100);
    var response = controller.WhatsUp(new WhatsupRequestModel { SessionId = sessionId });
    Console.WriteLine($"Game created with id = [{sessionId}]. Response: [{response}].");
}