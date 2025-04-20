using System.Diagnostics.CodeAnalysis;
using Game.Bots;
using Game.Model;
using Game.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Shared.Cards;
using Shared.GameEvents;
using Shared.Model;

namespace Blazor.Components.Pages.GameMonitor;

public partial class GameMonitor : IAsyncDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public GameRepository GameRepository { get; set; } = null!;
    [Inject] public IGameEvents GameEvents { get; set; } = null!;


    private Game.Logic.Game? _game;

    private ShowGame? _showGameComponent;
    private HubConnection? _hubConnection;
    private bool _initializing = true;
    private bool _lastRevealDone;


    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(GameId))
        {
            _game = GameRepository.CreateGame(GameEvents);
            GameId = _game.Id;
            NavigationManager.NavigateTo($"/game-monitor/{GameId}");

        }
        else if (_game == null)
        {
            _game = GameRepository.GetGame(GameId);
            if (_game?.Id == GameRepository.DeveloperGameId)
            {
                await PlayAgainAsync();
            }
        }

        await InitializeHubAsync();

        _initializing = false;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private async Task InitializeHubAsync()
    {
        if (_game == null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri(IGameEvents.HubUrl))
            .Build();

        _hubConnection.On<string>(IGameEvents.EventNames.PlayerJoined, async playerId =>
        {
            await JsRuntime.InvokeVoidAsync("playSound", "sound-join");
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On<string>(IGameEvents.EventNames.PlayerLeft, async playerId =>
        {
            // TODO: Different sound
            await JsRuntime.InvokeVoidAsync("playSound", "sound-join");
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On(IGameEvents.EventNames.NewRound, async () =>
        {
            if (_game == null) return;
            foreach (var player in _game.AlivePlayers)
            {
                if (player is BotPlayer bot)
                {
                    await bot.NewGameStateAsync(_game);
                }
            }
        });
        _hubConnection.On<GameState>(IGameEvents.EventNames.GameStateChanged, async (newState) =>
        {
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On(IGameEvents.EventNames.RoundResultsCompleted, async () =>
        {
            await _showGameComponent!.RoundResultsCompletedAsync();
        });
        _hubConnection.On<string, Card>(IGameEvents.EventNames.CardPlayed, async (playerId, card) =>
        {
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On<GameState>(IGameEvents.EventNames.RevealDone, async (gameStateWhenRevealed) =>
        {
            // TODO: _lastRevealDone is set too early
            if (gameStateWhenRevealed == GameState.Ended) _lastRevealDone = true;
            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync(IGameEvents.JoinGameMethod, _game.Id);
    }

    private async Task MaybeResetDevGameAsync()
    {
        _game = await GameRepository.GetOrCreateDevelopmentGameAsync(GameEvents, new ForceRecreate(true));
    }

    private async Task PlayAgainAsync()
    {
        if (_game == null) return;

        await _game.RestartAsync();

        if (_game.Id == GameRepository.DeveloperGameId)
        {

#pragma warning disable CS0618 // Type or member is obsolete
            await _game.AddPlayerAsync(new RandomBot("mrrandom", Character.Random(_game.AllCharacters())));
            await _game.AddPlayerAsync(new TriggerHappyBot("mrtrigger", Character.Random(_game.AllCharacters())));
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        await Task.Delay(300); // TODO: To let Blazor find peace
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();
    }
}