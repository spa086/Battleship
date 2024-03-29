﻿using BattleshipApi;
using BattleshipLibrary;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BattleshipTests.Web;

public class WebTests
{
    private readonly Controller controller;
    private readonly GamePool gamePool;
    private readonly TestingEnvironment testingEnvironment;
    private readonly WebResult webResult;

    public WebTests()
    {
        var serviceProvider = TestServiceCollection.Web().BuildServiceProvider();
        
        gamePool = serviceProvider.GetService<GamePool>()!;
        testingEnvironment = serviceProvider.GetService<TestingEnvironment>()!;
        controller = serviceProvider.GetService<Controller>()!;
        webResult = serviceProvider.GetService<WebResult>()!;
    }

    

    [SetUp]
    public void SetUp() => gamePool.ClearGames();

    [Test]
    public void CreatingShipsWhenNoGameStarted()
    {
        var request = SingleShipFleetCreationRequest(8755, new[] { new LocationModel { x = 1, y = 1 } });
        
        var exception = Assert.Throws<Exception>(() => controller.CreateFleet(request))!;
        
        Assert.That(exception.Message, Is.EqualTo($"User [8755] does not participate in any ongoing games."));
    }
    
    [Test]
    public void SettingGuestName()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);
        var request = SingleShipFleetCreationRequest(2, new[] { new LocationModel { x = 1, y = 1 } });
        request.userName = "Rachel";

        controller.CreateFleet(request);

        Assert.That(game.Guest!.Name, Is.EqualTo("Rachel"));
    }

    [Test]
    public void SettingHostName()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);
        var request = SingleShipFleetCreationRequest(1, new[] { new LocationModel { x = 1, y = 1 } });
        request.userName = "Boris";

        controller.CreateFleet(request);

        Assert.That(game.Host.Name, Is.EqualTo("Boris"));
    }

    [Test]
    public void SimpleGettingRequestModel()
    {
        var result = webResult.GetRequestModel<int>("5");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void PrepareErrorResult()
    {
        var result = webResult.Prepare<int, int>((_, _) =>
            throw new Exception("Some error."), "5");

        Assert.That(result, Is.EqualTo("\"Some error.\""));
    }

    [Test]
    public void PrepareResult()
    {
        var result = webResult.Prepare<int, int>((_, _) => 2, "5");

        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void SurrenderingWhenGameIsCancelledBeforeMatching()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.Cancelled, 3);
        game.Guest = null;
        
        Assert.DoesNotThrow(() => controller.AbortGame(3));
    }

[Test]
    public void SurrenderingWhenWaitingForGuest()
    {
        var game = testingEnvironment.CreateNewTestableGame(GameState.WaitingForGuest, 1, 2);

        controller.AbortGame(1);

        Assert.That(game.State, Is.EqualTo(GameState.Cancelled));
        Assert.That(game.Timer, Is.Null);
    }

    [Test]
    public void Surrendering([Values] GameState state)
    {
        if (state is GameState.HostWon or GameState.GuestWon or GameState.Cancelled or GameState.WaitingForGuest) 
            return;
        var game = testingEnvironment.CreateNewTestableGame(state, 1, 2);

        controller.AbortGame(1);

        Assert.That(game.State, Is.EqualTo(GameState.GuestWon));
        Assert.That(game.Timer, Is.Null);
    }

    [Test]
    public void TwoDecksOfSameShipAreInTheSameLocation()
    {
        testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);

        var exception = Assert.Throws<Exception>(() =>
            controller.CreateFleet(SingleShipFleetCreationRequest(1,
            new[] { new LocationModel { x = 1, y = 1 }, new LocationModel { x = 1, y = 1 } })))!;

        Assert.That(exception.Message, Is.EqualTo("Two decks are at the same place: [1,1]."));
    }

    [Test]
    public void GuestCreatesFleet()
    {
        var testableGame = testingEnvironment.CreateNewTestableGame(
            GameState.OnePlayerCreatesFleet, 1, 2);

        var result = controller.CreateFleet(new FleetCreationRequestModel
        { ships = new[] { NewSimpleShipForFleetCreationRequest(5, 5) }, userId = 2 });

        Assert.That(result, Is.False);
        var ship = testableGame.Guest!.Fleet.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var pair = ship.Decks.AssertSingle();
        Assert.That(pair.Key, Is.EqualTo(new Cell(5, 5)));
        Assert.That(pair.Value, Is.Not.Null);
        Assert.That(pair.Value.Location, Is.EqualTo(new Cell(5, 5)));
        Assert.That(pair.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.HostTurn));
    }

    [Test]
    public void HostCreatesFleet()
    {
        var testableGame = 
            testingEnvironment.CreateNewTestableGame(GameState.BothPlayersCreateFleets, 1, 2);

        var result = controller.CreateFleet(new FleetCreationRequestModel
        { ships = new[] { NewSimpleShipForFleetCreationRequest(1, 1) }, userId = 1 });

        Assert.That(result, Is.True);
        var ship = testableGame.Host.Fleet.AssertSingle();
        Assert.That(ship, Is.Not.Null);
        var deck = ship.Decks.AssertSingle();
        Assert.That(deck.Key, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value, Is.Not.Null);
        Assert.That(deck.Value.Location, Is.EqualTo(new Cell(1, 1)));
        Assert.That(deck.Value.Destroyed, Is.False);
        Assert.That(testableGame.State, Is.EqualTo(GameState.OnePlayerCreatesFleet));
    }

    private static ShipForCreationModel NewSimpleShipForFleetCreationRequest(int x, int y) =>
        new() { decks = new[] { new LocationModel { x = x, y = y } } };

    private static FleetCreationRequestModel SingleShipFleetCreationRequest(int userId,
#pragma warning disable CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
        LocationModel[]? decks) =>
        new() { userId = userId, ships = new[] { new ShipForCreationModel { decks = decks } } };
#pragma warning restore CS8601 // Возможно, назначение-ссылка, допускающее значение NULL.
}
