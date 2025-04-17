using Game.Bots;
using Game.Model;
using Game.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.Cards;

namespace Blazor.Components.Pages.GameMonitor;

public partial class GameMonitor
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public GameRepository GameRepository { get; set; } = null!;


    private Game.Logic.Game? _game;


    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(GameId))
        {
            // TODO: Remove rules
            var rules = new Rules() { CoinsToWin = 1, MinimumPlayerCount = 2 };
            _game = GameRepository.CreateGame(rules);
            GameId = _game.Id;
            NavigationManager.NavigateTo($"/game-monitor/{GameId}");

        }
        else if (_game == null)
        {
            _game = GameRepository.GetGame(GameId);
            if (_game?.Id == GameRepository.DeveloperGameId)
            {

                _game.AddPlayer(new RandomBot("mrrandom", Character.Random(_game.AllCharacters())));
            }
        }
    }

    private void PlayerCountChanged()
    {
        InvokeAsync(async () => await JsRuntime.InvokeVoidAsync("playSound", "sound-join"));
        InvokeAsync(StateHasChanged);
    }

    private void MaybeResetDevGame()
    {
        _game = GameRepository.GetOrCreateDevelopmentGame(new ForceRecreate(true));
    }

    private async Task PlayAgain()
    {
        _game?.Restart();
        await Task.Delay(300); // TODO: To let Blazor find peace
        StateHasChanged();
    }
}