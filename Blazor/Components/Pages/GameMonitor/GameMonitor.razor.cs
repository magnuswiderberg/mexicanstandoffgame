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

        // Player joined the game
        _hubConnection.On<string>(IGameEvents.EventNames.PlayerJoined, async playerId =>
        {
            await JsRuntime.InvokeVoidAsync("playSound", "sound-join");
            await InvokeAsync(StateHasChanged);
        });

        // Player left the game
        _hubConnection.On<string>(IGameEvents.EventNames.PlayerLeft, async playerId =>
        {
            // TODO: Different sound
            await JsRuntime.InvokeVoidAsync("playSound", "sound-join");
            await InvokeAsync(StateHasChanged);
        });

        // New round started
        _hubConnection.On(IGameEvents.EventNames.NewRound, async () =>
        {
            if (_game == null) return;
            await NewGameStateForBotsAsync();

        });

        // Game state changed: Created, Playing, Ended, etc.
        _hubConnection.On<GameState>(IGameEvents.EventNames.GameStateChanged, async (newState) =>
        {
            if (newState == GameState.Playing && _game.State != GameState.Playing)
            {
                await _game.StartNewRoundAsync(); // Get the game going
            }
            await InvokeAsync(StateHasChanged);
        });

        // Round results completed (from Game.SetRoundCompletedAsync(), from ShowGame.razor.cs)
        _hubConnection.On(IGameEvents.EventNames.RoundResultsCompleted, async () =>
        {
            await _showGameComponent.RoundResultsCompletedAsync();
        });

        // Card played by a player
        _hubConnection.On<string, Card>(IGameEvents.EventNames.CardPlayed, async (playerId, card) =>
        {
            await InvokeAsync(StateHasChanged);
        });

        // ShowGame.razor.cs offers buttons or speech to reval each player's action.
        // After all that, this event happens.
        _hubConnection.On<GameState>(IGameEvents.EventNames.RevealDone, async gameStateWhenRevealed =>
        {
            if (_game == null) return;

            if (gameStateWhenRevealed == GameState.Ended)
            {
                await _showGameComponent.WaitForLastRevealAsync();

                _lastRevealDone = true;
                await InvokeAsync(StateHasChanged);
                await SpeakWinner();
            }
            else
            {
                await _game.StartNewRoundAsync();
            }
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync(IGameEvents.JoinGameMethod, _game.Id);
    }

    private async Task NewGameStateForBotsAsync()
    {
        if (_game == null) return;

        foreach (var player in _game.AlivePlayers)
        {
            if (player is BotPlayer bot)
            {
                await bot.NewGameStateAsync(_game);
            }
        }
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
            var baseUrl = NavigationManager.BaseUri.TrimEnd('/');
            await _game.AddPlayerAsync(new ApiBot(new Uri($"{baseUrl}/api/bots/mrgold"), PlayerId.From("51E97B98-D650-4D55-B8CF-80052732767C"), "Mr. Gold", Character.Random(_game.AllCharacters())));
            await _game.AddPlayerAsync(new ApiBot(new Uri($"{baseUrl}/api/bots/mrrandom"), PlayerId.From("02D7AF25-A3F4-45A1-A977-BDCF51D93F67"), "Mrs. Stochastic", Character.Random(_game.AllCharacters())));
            await _game.AddPlayerAsync(new ApiBot(new Uri($"{baseUrl}/api/bots/mrtrigger"), PlayerId.From("02E70BA2-3D88-4C7D-A426-DCC8A007F1A1"), "Mr. Trigger", Character.Random(_game.AllCharacters())));
        }
        
        await Task.Delay(300); // To let Blazor find peace
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();
    }
}