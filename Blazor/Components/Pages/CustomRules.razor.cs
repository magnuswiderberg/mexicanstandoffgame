using Game.Model;
using Game.Repository;
using Microsoft.AspNetCore.Components;
using Shared.GameEvents;

namespace Blazor.Components.Pages;

public partial class CustomRules
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public GameRepository GameRepository { get; set; } = null!;
    [Inject] public IGameEvents GameEvents { get; set; } = null!;

    private const int MaxPlayers = 8;

    private readonly Rules _rules = new();
    private int _maxOnChest = 1;

    protected void CreateCustomizedGame()
    {
        var chestsPerPlayerCount = new Dictionary<int, int>();
        for (var playerCount = 0; playerCount <= MaxPlayers; playerCount++)
        {
            chestsPerPlayerCount.Add(playerCount, _maxOnChest);
        }

        _rules.ChestsPerPlayerCount = chestsPerPlayerCount;
        
        var game = GameRepository.CreateGame(GameEvents, _rules);
        NavigationManager.NavigateTo($"game-monitor/{game.Id}");
    }
}