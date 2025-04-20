using Game.Model;
using Game.Repository;
using Moq;
using Shared.Cards;
using Shared.GameEvents;
using Shared.Model;
using Shouldly;

#pragma warning disable CA2007

namespace UnitTests;

public class GameEventTests
{
    private readonly GameRepository _gameRepository = new();

    private readonly Player _player1 = new("1", Character.Get(1)!);
    private readonly Player _player2 = new("2", Character.Get(2)!);
    private readonly Player _player3 = new("3", Character.Get(3)!);

    private readonly Game.Logic.Game _game;

    private readonly Mock<IGameEvents> _gameEventsMock = new();

    public GameEventTests()
    {
        _game = _gameRepository.CreateGame(_gameEventsMock.Object);
    }

    [Fact]
    public async Task Game_Monitor_Gets_Notified_When_Player_Joins()
    {
        _gameEventsMock.Setup(m => m.PlayerJoinedAsync(_game.Id, _player1.Id)).Verifiable();
        await _game.AddPlayerAsync(_player1);
        _gameEventsMock.Verify();
    }

    [Fact]
    public async Task Game_Monitor_Gets_Notified_When_Player_Leaves()
    {
        await _game.AddPlayerAsync(_player1);
        _gameEventsMock.Setup(m => m.PlayerLeftAsync(_game.Id, _player1.Id)).Verifiable();
        await _game.RemovePlayerAsync(_player1);
    }

    [Fact]
    public async Task Monitor_Is_Notified_When_Game_Starts()
    {
        await _game.AddPlayerAsync(_player1);
        await _game.AddPlayerAsync(_player2);
        await _game.AddPlayerAsync(_player3);
        _gameEventsMock.Setup(m => m.GameStateChangedAsync(_game.Id, GameState.Playing)).Verifiable();
        await _game.StartAsync();
        _gameEventsMock.Verify();
    }

    [Fact]
    public async Task Monitor_Is_Notified_When_Game_Ends()
    {
        await _game.AddPlayerAsync(_player1);
        await _game.AddPlayerAsync(_player2);
        await _game.AddPlayerAsync(_player3);
        await _game.StartAsync();
        _gameEventsMock.Setup(m => m.GameStateChangedAsync(_game.Id, GameState.Ended)).Verifiable();
        await MakeGameEnd();
        _gameEventsMock.Verify();
    }

    private async Task MakeGameEnd()
    {
        while (_game.State != GameState.Ended)
        {
            var firstPlayer = _game.Players[0];
            foreach (var player in _game.AlivePlayers)
            {
                await player.SetSelectedCardAsync(player.Equals(firstPlayer) ? Card.Chest : Card.Dodge);
            }
        }
    }

    [Fact]
    public async Task Monitor_Is_Notified_When_Player_Selects_Card()
    {
        await _game.AddPlayerAsync(_player1);
        await _game.AddPlayerAsync(_player2);
        await _game.StartAsync();
        var monitorNotified = false;
        _player1.CardChanged = player => { 
            monitorNotified = Equals(player, _player1); 
            return Task.CompletedTask;
        };
        await _player1.SetSelectedCardAsync(Card.Load);
        monitorNotified.ShouldBeTrue();
    }

    [Fact]
    public async Task Monitor_Is_Notified_When_Player_Unselects_Card()
    {
        await _game.AddPlayerAsync(_player1);
        await _game.AddPlayerAsync(_player2);
        await _game.StartAsync();
        await _player1.SetSelectedCardAsync(Card.Load);
        var monitorNotified = false;
        _player1.CardChanged = player =>
        {
            monitorNotified = Equals(player, _player1);
            return Task.CompletedTask;
        };
        await _player1.SetSelectedCardAsync(null);
        monitorNotified.ShouldBeTrue();
    }

    //[Fact]
    //public void Todo_Select_Card_Timeout()
    //{
    //    // TODO
    //}

    //[Fact]
    //public void Todo_Round_Completed()
    //{
    //    // TODO
    //}

    //[Fact]
    //public void Todo_Restart()
    //{
    //    // TODO
    //}
}