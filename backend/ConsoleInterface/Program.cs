using BattleshipApi;
using BattleShipLibrary;

var controller = new Controller();
int? lastSessionId = null;
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
    throw new NotImplementedException();
}

void ProcessAttackDecision()
{
    Console.WriteLine("Please enter session id: ");
    var sessionId = ReadCell();
    Console.WriteLine("Where to attack? Enter location: ");
    var attackocation = ReadCell();
    var result = controller.Attack(
        new AttackRequestModel 
        { 
            location = new LocationModel { x = attackocation.x, y = attackocation.y } 
        });
    Console.WriteLine($"Attacked location [{attackocation}]. Result: [{result.result}]. Game id = [{sessionId}].");
}

string GetBoolString(bool isDestroyed) => isDestroyed ? "t" : "F";

#pragma warning disable CS8321 // Локальная функция объявлена, но не используется
string GetFleetString(IEnumerable<Ship> fleet) => 
    string.Join("; ", fleet.Select(ship =>
    {
        var shipStr = string.Join(", ",
            ship.Decks.Select(deck => $"{deck.Key}{GetBoolString(deck.Value.Destroyed)}"));
        return $"[{shipStr}]";
    }));
#pragma warning restore CS8321 // Локальная функция объявлена, но не используется

void PrepareConsole()
{
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
    Console.WriteLine($"Welcome to BATTLESHIPS by FIERCE DEVELOPMENT!");
    throw new NotImplementedException();
    //var sessionInfoString = sessionModels.Select(x => $"[id={x.sessionId};state={x.State}]");
    //if (sessionInfoString.Any())
    //    Console.WriteLine($"Ongoing sessions: [{string.Join(", ", sessionInfoString)}].");
    //ShowLastSessionIfExists(lastSessionId);
    //var optionsDescription = "Do you wish to create a game (g), join existing one (j)";
    //if (sessionModels.Any(x => x.State == GameState.BothPlayersCreateFleets || 
    //    x.State == GameState.WaitingForPlayer2ToCreateFleet))
    //    optionsDescription += " or create ships (s)";
    //if (sessionModels.Any(x => x.State == GameState.Player1Turn || x.State == GameState.Player2Turn))
    //    optionsDescription += " or attack (a)";
    //optionsDescription += "?";
    //Console.WriteLine(optionsDescription);
}

#pragma warning disable CS8321 // Локальная функция объявлена, но не используется
void ShowLastSessionIfExists(int? lastSessionId)
{
    throw new NotImplementedException();
    //if (lastSessionId.HasValue)
    //{
    //    var game = GamePool.Games[lastSessionId.Value];
    //    if (game.State == GameState.Player1Turn || game.State == GameState.Player2Turn)
    //    {
    //        Console.WriteLine($"Last session id is: [{lastSessionId}].");
    //        var player1FleetStr = GetFleetString(game!.Player1Ships!);
    //        var player2FleetStr = GetFleetString(game!.Player2Ships!);
    //        //todo check 3 times
    //        Console.WriteLine($"Player 1 ships after attack: {{{player1FleetStr}}}.");
    //        Console.WriteLine($"Player 2 ships after attack: {{{player2FleetStr}}}.");
    //    }
    //}
}
#pragma warning restore CS8321 // Локальная функция объявлена, но не используется

void ProcessCreateShipsDecision()
{
    throw new NotImplementedException();
    //Console.WriteLine("Which one? Enter id: ");
    //var idStr = Console.ReadLine();
    //var id = int.Parse(idStr!);
    //Console.WriteLine("Enter ships in format \"[d1,d2,..];[e1,e2,..];..\" " +
    //            "where di, ei etc. are deck coordinates...");
    //var decksArrays = Console.ReadLine()!.Split(";")
    //    .Select(x => x[1..^1]).ToArray()
    //    .Select(shipString =>
    //    {
    //        var decksStrings = shipString.Split(',');
    //        var decks = decksStrings.Select(deckString => int.Parse(deckString)).ToArray();
    //        return new ShipTransportModel { decks = decks };
    //    }).ToArray();
    //var response = controller.CreateFleet(new FleetCreationRequestModel
    //    { userId = id, ships = decksArrays });
    //Console.WriteLine($"Ships created with game id = [{id}]. Response: [{response}].");
}

void ProcessJoinGameDecision()
{
    Console.WriteLine("Which one? Enter id: ");
    //todo check 3 times
    var idStr = Console.ReadLine();
    var id = int.Parse(idStr!);
    var response = controller.WhatsUp(new WhatsupRequestModel { userId = id });
    lastSessionId = id;
    Console.WriteLine($"Joined game with id = [{id}]. Response: [{response}].");
}

void ProcessCreateGameDecision()
{
    var sessionId = new Random().Next(100);
    var response = controller.WhatsUp(new WhatsupRequestModel { userId = sessionId });
    lastSessionId = sessionId;
    Console.WriteLine($"Game created with id = [{sessionId}]. Response: [{response}].");
}