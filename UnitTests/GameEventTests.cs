using Game.Events;
using Game.Model;
using Game.Repository;
using Shared.Cards;
using Shouldly;

namespace UnitTests;

public class GameEventTests
{
    private readonly GameRepository _gameRepository = new();

    private readonly Player _player1 = new("1", Character.Get(1)!);
    private readonly Player _player2 = new("2", Character.Get(2)!);
    private readonly Player _player3 = new("3", Character.Get(3)!);

    private readonly Game.Logic.Game _game;

    public GameEventTests()
    {
        _game = _gameRepository.CreateGame();
    }

    [Fact]
    public void Game_Monitor_Gets_Notified_When_Player_Joins()
    {
        var monitorNotified = false;
        _game.PlayerJoined += (sender, args) => { monitorNotified = Equals((args as PlayerEvent)?.Player, _player1); };
        _game.AddPlayer(_player1);
        monitorNotified.ShouldBeTrue();
    }

    [Fact]
    public void Game_Monitor_Gets_Notified_When_Player_Leaves()
    {
        _game.AddPlayer(_player1);
        var monitorNotified = false;
        _game.PlayerLeft += (sender, args) => { monitorNotified = Equals((args as PlayerEvent)?.Player, _player1); };
        _game.RemovePlayer(_player1);
        monitorNotified.ShouldBeTrue();
    }

    //[Fact]
    //public void Abort_Game_If_Monitor_Is_Removed()
    //{
    //    _game.State.ShouldBe(GameState.Created);
    //    _game.AddPlayer(_player1);
    //    _game.SetMonitorId("mon");
    //    _game.State.ShouldBe(GameState.Created);
    //    _game.RemoveMonitor();
    //    _game.State.ShouldBe(GameState.Aborted);
    //}

    [Fact]
    public void Monitor_Is_Notified_When_Game_Starts()
    {
        _game.AddPlayer(_player1);
        _game.AddPlayer(_player2);
        _game.AddPlayer(_player3);
        var monitorNotified = false;
        _game.GameStateChanged += (sender, args) => { monitorNotified = _game.State == GameState.Playing; };
        _game.Start();
        monitorNotified.ShouldBeTrue();
    }

    [Fact]
    public void Monitor_Is_Notified_When_Game_Ends()
    {
        _game.AddPlayer(_player1);
        _game.AddPlayer(_player2);
        _game.AddPlayer(_player3);
        _game.Start();
        var monitorNotified = false;
        _game.GameStateChanged += (sender, args) => { monitorNotified = _game.State == GameState.Ended; };
        MakeGameEnd();
        monitorNotified.ShouldBeTrue();
    }

    private void MakeGameEnd()
    {
        while (_game.State != GameState.Ended)
        {
            var firstPlayer = _game.Players[0];
            foreach (var player in _game.AlivePlayers)
            {
                player.SetSelectedCard(player.Equals(firstPlayer) ? Card.Chest: Card.Dodge);
            }
        }
    }

    [Fact]
    public void Monitor_Is_Notified_When_Player_Selects_Card()
    {
        _game.AddPlayer(_player1);
        _game.AddPlayer(_player2);
        _game.Start();
        var monitorNotified = false;
        _player1.CardChanged += (sender, args) => { monitorNotified = Equals((args as PlayerEvent)?.Player, _player1); };
        _player1.SetSelectedCard(Card.Load);
        monitorNotified.ShouldBeTrue();
    }

    [Fact]
    public void Monitor_Is_Notified_When_Player_Unselects_Card()
    {
        _game.AddPlayer(_player1);
        _game.AddPlayer(_player2);
        _game.Start();
        _player1.SetSelectedCard(Card.Load);
        var monitorNotified = false;
        _player1.CardChanged += (sender, args) => { monitorNotified = Equals((args as PlayerEvent)?.Player, _player1); };
        _player1.SetSelectedCard(null);
        monitorNotified.ShouldBeTrue();
    }

    [Fact]
    public void Todo_Select_Card_Timeout()
    {
        // TODO
    }

    [Fact]
    public void Todo_Round_Completed()
    {
        // TODO
    }

    [Fact]
    public void Todo_Restart()
    {
        // TODO
    }
}