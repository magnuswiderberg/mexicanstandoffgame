using System.Diagnostics.CodeAnalysis;
using Common.Cards;
using Common.GameEvents;
using Common.Model;
using Game.Bots;
using Game.Repository;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Blazor.Components.Pages.GameMonitor;

public partial class GameMonitor : IAsyncDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public GameRepository GameRepository { get; set; } = null!;
    [Inject] public IGameEvents GameEvents { get; set; } = null!;


    private Game.Logic.Game? _game;

    private ShowGame _showGameComponent = null!;
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
            await _showGameComponent.RoundResultsCompletedAsync();
        });
        _hubConnection.On<string, Card>(IGameEvents.EventNames.CardPlayed, async (playerId, card) =>
        {
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On<GameState>(IGameEvents.EventNames.RevealDone, async gameStateWhenRevealed =>
        {
            if (_game == null) return;
            //await GameEvents.NewRoundAsync(_game.Id);

            if (gameStateWhenRevealed == GameState.Ended)
            {
                await _showGameComponent.WaitForLastRevealAsync();

                _lastRevealDone = true;
                await InvokeAsync(StateHasChanged);
                await SpeakWinner();
            }
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync(IGameEvents.JoinGameMethod, _game.Id);
    }
    
    private async Task SpeakWinner()
    {
        if (_game == null) return;
        if (_showGameComponent.UseSpeech() == false) return;

        var winnerStart = Random.Shared.Next(0, 3) switch
        {
            0 => "Tallying the results ...",
            1 => "Game results are in!",
            _ => "The game is over!"
        };
        switch (_game.Winners.Count)
        {
            case 0:
                await JsRuntime.InvokeVoidAsync("Speak", $"{winnerStart} No winner this game...");
                break;
            case 1:
                await JsRuntime.InvokeVoidAsync("Speak", $"{winnerStart} And the winner is ... {_game.Winners[0].Name}!");
                break;
            default:
            {
                var winners = string.Join(" and ", _game.Winners.Select(x => x.Name));
                await JsRuntime.InvokeVoidAsync("Speak", $"{winnerStart} And the winners are ... {winners}!");
                break;
            }
        }
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
            await _game.AddPlayerAsync(new RandomBot("Mr Random", Character.Random(_game.AllCharacters())));
            await _game.AddPlayerAsync(new TriggerHappyBot("Mr Trigger", Character.Random(_game.AllCharacters())));
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