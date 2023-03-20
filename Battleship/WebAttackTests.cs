﻿using BattleshipApi;
using BattleshipLibrary;
using NUnit.Framework;

namespace BattleshipTests;

public class WebAttackTests
{
    [SetUp]
    public void SetUp() => GamePool.ClearGames();

    [Test]
    public void AttackingInOpponentsTurn()
    {
        TestingEnvironment.CreateNewTestableGame(GameState.Player1Turn, 1, 2);

        var exception = Assert.Throws<Exception>(() => CreateController().Attack(
            new AttackRequestModel { location = new LocationModel { x = 5, y = 6 },userId = 2 }));

        Assert.That(exception.Message, Is.EqualTo("Not your turn."));
    }

    [Test]
    public void ReturningExcludedLocationsFor([Values] bool firstPlayer)
    {
        TestingEnvironment.CreateNewTestableGame(
            firstPlayer ? GameState.Player1Turn : GameState.Player2Turn,
            1, 2);

        var result = CreateController().Attack(
            new AttackRequestModel 
            { 
                location = new LocationModel { x = 5, y = 6 }, userId = firstPlayer ? 1 : 2
            });

        var location = (firstPlayer ? result.excludedLocations1 : result.excludedLocations2)
            .AssertSingle();
        Assert.That(location.x, Is.EqualTo(5));
        Assert.That(location.y, Is.EqualTo(6));
    }

    [Test]
    public void AttackMissed()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 1, 2,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, new[] { new Cell(3, 3) }, 2));

        var result = CreateController().Attack(new AttackRequestModel 
            { location = new LocationModel { x = 2, y = 2 }, userId = 1 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Missed));
        Assert.That(game.State, Is.EqualTo(GameState.Player2Turn));
    }

    [Test]
    public void AttackHitsAShip()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 1, 2,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, 
            new[] {new Cell(2, 2), new Cell(2, 3) }, 2));
        //todo put controller into variable?
        var request = new AttackRequestModel
            { location = new LocationModel { x = 2, y = 2 }, userId = 1 };

        var result = CreateController().Attack(request);

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Hit));
        var decks = game.SecondFleet.AssertSingle().Decks.Values;
        Assert.That(decks, Has.Count.EqualTo(2));
        Assert.That(decks.Where(x => x.Location == new Cell(2,2)).AssertSingle().Destroyed, Is.True);
        Assert.That(decks.Where(x => x.Location == new Cell(2,3)).AssertSingle().Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Turn));
    }

    [Test]
    public void AttackKillsAShip()
    {
        SetupGameInPoolWithState(GameState.Player2Turn, 1, 2, game => game.SetupFleets(
            new List<Ship> 
            {
                new Ship{Decks = GenerateDeckDictionary(0,0) },
                new Ship{Decks = GenerateDeckDictionary(2,2) }
            },
            new List<Ship> {new Ship{Decks = GenerateDeckDictionary(2,2)}}));

        var result = CreateController().Attack(
            new AttackRequestModel { location = new LocationModel { x = 0, y = 0 }, userId = 2 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Killed));
    }

    [Test]
    public void Playe21AttacksAndWins()
    {
        var game = SetupGameInPoolWithState(GameState.Player2Turn, 1, 2,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1,
            new[] { new Cell(2, 2) }, 2));

        //todo put controller into field?
        var result = CreateController().Attack(new AttackRequestModel
        { location = new LocationModel { x = 1, y = 1 }, userId = 2 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        //todo check 3 times
        Assert.That(game.FirstFleet!.Single().Decks.Single().Value.Destroyed, Is.True);
        Assert.That(game.SecondFleet!.Single().Decks.Single().Value.Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player2Won));
    }

    [Test]
    public void Player1AttacksAndWins()
    {
        var game = SetupGameInPoolWithState(GameState.Player1Turn, 1, 2,
            game => game.SetupSimpleFleets(new[] { new Cell(1, 1) }, 1, 
            new[] { new Cell(2, 2)}, 2));

        //todo put controller into variable?
        var result = CreateController().Attack(new AttackRequestModel
            { location = new LocationModel { x = 2, y = 2 }, userId = 1 });

        Assert.That(result.result, Is.EqualTo(AttackResultTransportModel.Win));
        //todo check 3 times
        Assert.That(game.SecondFleet!.Single().Decks.Single().Value.Destroyed, Is.True);
        Assert.That(game.FirstFleet!.Single().Decks.Single().Value.Destroyed, Is.False);
        Assert.That(game.State, Is.EqualTo(GameState.Player1Won));
    }

    private static TestableGame SetupGameInPoolWithState(GameState state, int firstUserId,
        int? secondUserId = null, Action<TestableGame>? modifier = null)
    {
        var game = new TestableGame(firstUserId);
        if (secondUserId != null) game.SetSecondUserId(secondUserId);
        game.SetState(state);
        modifier?.Invoke(game);
        GamePool.SetGame(game);
        return game;
    }

    private static Controller CreateController() => new();

    private static Dictionary<Cell, Deck> GenerateDeckDictionary(int x, int y)
    {
        var result = new Dictionary<Cell, Deck> { { new Cell(x, y), new Deck(x, y, false) } };
        return result;
    }
}
