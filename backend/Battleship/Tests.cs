using NUnit.Framework;
using BattleShipLibrary;
using BattleshipApi;

namespace Battleship;

//lines 105 chars
//methods 20 lines
//files 200 lines
//no partial
//project 20 files

public class Tests
{
    //todo tdd throw if any location list is uninitialized
    //todo tdd throw if two ships in same location
    //todo tdd throw if ships are adjacent
    //todo tdd game cycle
    //todo tdd field borders (and what if nowhere left to fire?)
    //todo tdd 2nd dimension
    //todo INPRO ASP project
    //todo console interface

    private readonly TestableGame game = new(0);
     
    [TearDown]
    public void TearDown() => GamePool.SetGame(null);

    [SetUp]
    public void SetUp() => game.StandardSetup();

    [Test]
    public void CreateFleetByController()
    {
        GamePool.SetGame(new TestableGame(0).SetupStarted());

        new Controller().CreateFleet(new[] { new ShipFrontModel { Decks = new[] { 1 } } });

        var ship = game.Player1Ships.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(1));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(1));
        Assert.That(deck.Value.Destroyed, Is.False);
    }

    [Test]
    public void StartingAGameByController()
    {
        new Controller().WhatsUp(new WhatsupRequestModel { SessionId = 0});

        Assert.That(GamePool.TheGame, Is.Not.Null);
        Assert.That(GamePool.TheGame.Started, Is.False);
    }

    [Test]
    public void WhatsUpAfterSecondPlayerJoins()
    {
        var game = new TestableGame(0);
        game.SetupStarted();
        GamePool.SetGame(game);

        AssertControllerReturnValue(x => x.WhatsUp(null), WhatsUpResponse.CreatingFleet);
    }

    //todo tdd whatsup when nobody connected yet - throw exception?

    [Test]
    public void WhatsUpBeforeSecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0));

        AssertControllerReturnValue(x => x.WhatsUp(new WhatsupRequestModel { SessionId = 0 }), WhatsUpResponse.WaitingForStart);
    }

    [Test]
    public void SecondPlayerJoins()
    {
        GamePool.SetGame(new Game(0));
        
        Assert.That(GamePool.StartPlaying(0), Is.True);

        Assert.That(GamePool.TheGame!.Started, Is.True);
    }

    [Test]
    public void StartingAGame()
    {
        var resut = GamePool.StartPlaying(0);

        Assert.That(resut, Is.False);
        Assert.That(GamePool.TheGame, Is.Not.Null);
        Assert.That(GamePool.TheGame.Started, Is.False);
    }

    [Test]
    public void CreateShipsSimple()
    {
        game.CreateAndSaveShips(new FleetCreationModel
        { Ships = new[] { new ShipCreationModel { Decks = new[] { 1, 2 } } }, IsForPlayer1 = true });

        //todo use separate collection
        var ship = game.Player1Ships.AssertSingle();
        var decks = ship.Decks;
        Assert.That(decks, Has.Count.EqualTo(2));
        var orderedDecks = decks.Values.OrderBy(x => x.Location);
        var deck1 = orderedDecks.First();
        Assert.That(deck1.Destroyed, Is.False);
        Assert.That(deck1.Location, Is.EqualTo(1));
        var deck2 = orderedDecks.Last();
        Assert.That(deck2.Destroyed, Is.False);
        Assert.That(deck2.Location, Is.EqualTo(2));
    }

    [Test]
    public void DamagingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        game.SetTurn(false);

        game.Attack(1);

        Assert.That(game.Player1Ships.AssertSingle().Decks[1].Destroyed);
    }

    //todo tdd this but for 1st player turn
    [Test]
    public void DestroyingAMultideckShip()
    {
        game.SetupSimpleFleets(new[] { 0, 1 }, new[] { 2 });
        game.Player1Ships.Single().Decks[1].Destroyed = true;
        game.SetTurn(false);

        game.Attack(0);

        var destroyedShip = game.Player1Ships.AssertSingle();
        Assert.That(destroyedShip.Decks.Values.All(x => x.Destroyed));
        Assert.That(Game.IsDestroyed(destroyedShip));
    }

    //todo similar for 2nd player
    [Test]
    public void AttackSamePlaceTwice()
    {
        game.SetupExcludedLocations(1);

        var exception = Assert.Throws<Exception>(() => game.Attack(1));
        Assert.That(exception.Message,
            Is.EqualTo("Location [1] is already excluded."));
    }

    //todo similar for 2nd player
    [Test]
    public void Miss()
    {
        game.Attack(0);

        Assert.That(game.Player1Turn, Is.False);
        Assert.That(game.Win, Is.False);
        game.Player2Ships.AssertSingle();
    }

    //todo similar for 2nd player
    [Test]
    public void Excluding()
    {
        game.Attack(144);

        //Assert.That(game.ExcludedLocations1.AssertSingle(), Is.EqualTo(144));
    }

    [Test]
    public void AttackAndWin()
    {
        game.Attack(2);

        game.ExcludedLocations1.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Player2Ships.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.Player1Turn);
    }

    [Test]
    public void Player2AttacksAndWins()
    {
        game.SetTurn(false);

        game.Attack(1);

        game.Player2Ships.AssertSingle();
        Assert.That(Game.IsDestroyed(game.Player1Ships.AssertSingle()));
        Assert.That(game.Win);
        Assert.That(game.Player1Turn, Is.False);
    }

    private static void AssertControllerReturnValue<T>(Func<Controller, T> controllerFunction,
        T expectedValue) =>
        Assert.That(controllerFunction(new Controller()), Is.EqualTo(expectedValue));
}