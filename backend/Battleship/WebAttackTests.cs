using BattleshipApi;
using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebAttackTests
{
    //[Test]
    //public void AttackReturnsField()
    //{
    //    var controller = CreateController();
    //    var result = controller.Attack(new AttackRequestModel { location = new LocationModel { x = 1, y = 1 } });

    //    var firstFleet = result.fleet1;
    //    Assert.That(firstFleet, Is.Not.Null);
    //    Assert.That(firstFleet.)
    //}

    [Test]
    public void AttackMissed()
    {
        var game = SetupGameInPoolWithState(GameState.Player2Turn, 0,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(3, 3) }, 2));

        var result = CreateController().Attack(new AttackRequestModel 
            { location = new LocationModel { x = 22, y = 22 }, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Missed));
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void AttackHitsAShip()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 0,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, 
            new[] {new Cell(2, 2), new Cell(2, 3) }, 2));

        //todo put controller into variable?
        var result = CreateController().Attack(
            new AttackRequestModel 
            { 
                location = new LocationModel { x=2, y=2}, userId = 0 
            });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Hit));
        var decks = game.SecondFleet.AssertSingle().Decks.Values;
        Assert.That(decks, Has.Count.EqualTo(2));
        //todo check 3 times
        Assert.That(decks.Where(x => x.Location == new Cell(2,2)).AssertSingle().Destroyed, Is.True);
        Assert.That(decks.Where(x => x.Location == new Cell(2,3)).AssertSingle().Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
    }

    [Test]
    public void AttackKillsAShip()
    {
        SetupGameInPoolWithState(GameState.Player2Turn, 0, game => game.SetupFleets(new List<Ship> {
            new Ship{Decks = new Dictionary<Cell, Deck> { { new Cell(0,0), new Deck(0,0, false) } }},
            new Ship{Decks = new Dictionary<Cell, Deck>{{ new Cell(2, 2), new Deck(2,2, false) }}}},
            new List<Ship>
            {
                new Ship
                {
                    Decks = new Dictionary<Cell, Deck> { { new Cell(2,2), new Deck(2,2, false) } }
                }
            }));

        var result = CreateController().Attack(new AttackRequestModel 
        { 
            location = new LocationModel { x=0, y=0}, userId = 0
        });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Killed));
    }

    //todo similar for player 2
    [Test]
    public void Player1AttacksAndWins()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 0,
            game => game.SetupSimpleFleets(new[] { new Cell(0, 0) }, 1, 
            new[] { new Cell(0, 2)}, 2));

        //todo put controller into variable?
        var result = CreateController().Attack(new AttackRequestModel
            { location = new LocationModel { x = 0, y = 2 }, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        Assert.That(game.Win, Is.True);
        //todo check 3 times
        Assert.That(game.SecondFleet!.Single().Decks.Single().Value.Destroyed, Is.True);
        Assert.That(game.FirstFleet!.Single().Decks.Single().Value.Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    private static TestableGame SetupGameInPoolWithState(GameState state, int sessionId,
        Action<TestableGame> modifier)
    {
        var game = new TestableGame(sessionId);
        game.SetState(state);
        modifier.Invoke(game);
        GamePool.SetGame(game);
        return game;
    }

    private static Controller CreateController() => new();
}
