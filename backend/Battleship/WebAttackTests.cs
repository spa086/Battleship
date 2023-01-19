using BattleshipApi;
using BattleShipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebAttackTests
{
    [Test]
    public void AttackMissed()
    {
        var game = SetupGameInPoolWithState(GameState.Player2Turn, 0,
            game => game.SetupSimpleFleets(new[] { 1 }, new[] { 2 }));

        var result = CreateController().Attack(new AttackRequestModel { Location = 22, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Missed));
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void AttackHitsAShip()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 0,
            game => game.SetupSimpleFleets(new[] { 1 }, new[] { 2, 3 }));

        //todo put controller into variable?
        var result = CreateController().Attack(new AttackRequestModel { Location = 2, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Hit));
        var decks = game.Player2Ships.AssertSingle().Decks.Values;
        Assert.That(decks, Has.Count.EqualTo(2));
        //todo check 3 times
        Assert.That(decks.Where(x => x.Location == 2).AssertSingle().Destroyed, Is.True);
        Assert.That(decks.Where(x => x.Location == 3).AssertSingle().Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
    }

    [Test]
    public void AttackKillsAShip()
    {
        SetupGameInPoolWithState(GameState.Player2Turn, 0, game => game.SetupFleets(new List<Ship> {
            new Ship{Decks = new Dictionary<int, Deck> { { 0, new Deck(0, false) } }},
            new Ship{Decks = new Dictionary<int, Deck>{{ 1, new Deck(1, false) }}}},
            new List<Ship>
            {
                new Ship
                {
                    Decks = new Dictionary<int, Deck> { { 2, new Deck(2, false) } }
                }
            }));

        var result = CreateController().Attack(new AttackRequestModel { Location = 0, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Killed));
    }

    //todo similar for player 2
    [Test]
    public void Player1AttacksAndWins()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 0,
            game => game.SetupSimpleFleets(new[] { 1 }, new[] { 2 }));

        //todo put controller into variable?
        var result = CreateController().Attack(new AttackRequestModel { Location = 2, userId = 0 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        Assert.That(game.Win, Is.True);
        //todo check 3 times
        Assert.That(game.Player2Ships!.Single().Decks.Single().Value.Destroyed, Is.True);
        Assert.That(game.Player1Ships!.Single().Decks.Single().Value.Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    private static TestableGame SetupGameInPoolWithState(GameState state, int sessionId,
        Action<TestableGame> modifier)
    {
        var game = new TestableGame(sessionId);
        game.SetState(state);
        modifier.Invoke(game);
        GamePool.SetGame(game, sessionId);
        return game;
    }

    private static Controller CreateController() => new();
}
