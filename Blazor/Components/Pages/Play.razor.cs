using System.Diagnostics.CodeAnalysis;
using Blazor.Components.Elements;
using Game.Model;
using Microsoft.AspNetCore.Components;
using Game.Repository;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shared.Cards;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.GameEvents;
using Shared.Model;

namespace Blazor.Components.Pages;

public partial class Play : IAsyncDisposable
{
    [Inject] public GameRepository GameRepository { get; set; } = null!;
    [Inject] public ProtectedLocalStorage ProtectedLocalStorage { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private Game.Logic.Game? _game;
    private Player? _player;
    private string? _playerId;
    private string? _playerName;
    private string? _playerNameInfo;

    private bool _gameIsFull;
    private bool _waitingForMonitor;
    //private string? _appearClassName;
    private bool _gameHasStartedAlready;
    //private string? _rejoinStatus;

    private bool _initializing = true;
    private HubConnection? _hubConnection;
    private bool _lastRevealDone;

    private bool _showMenu;
    private ConfirmDialog _confirmDialog = null!;

    private int _secondsLeft;
    private Timer? _timer;
    //private PeriodicTimer _timer = null!;
    private bool _firstTimerStarted;


    protected override async Task OnInitializedAsync()
    {
        _game = GameRepository.GetGame(GameId);
        if (_game == null) return;

        //_timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _timer = new Timer(_ =>
        {
            if (_game.State != GameState.Playing) return;
            if (_secondsLeft > 0) InvokeAsync(StateHasChanged);
            else
            {
                StopTimer();
                if (_player is { SelectedCard: null })
                {
                    // TODO: sometimes does not trigger game monitor
                    _player.SetSelectedCardAsync(Card.Dodge).Wait();
                    InvokeAsync(StateHasChanged);
                }
            }
            _secondsLeft--;
            if (_secondsLeft < 0) _secondsLeft = 0;
        });

        _waitingForMonitor = false;
        await InitializeHubAsync();
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private async Task InitializeHubAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri(IGameEvents.HubUrl))
            .Build();

        _hubConnection.On<string>(IGameEvents.EventNames.PlayerJoined, async playerId =>
        {
            await GameStateChangedAsync();
        });
        _hubConnection.On<string>(IGameEvents.EventNames.PlayerLeft, async playerId =>
        {
            await GameStateChangedAsync();
        });
        _hubConnection.On<GameState>(IGameEvents.EventNames.GameStateChanged, async newState =>
        {
            await GameStateChangedAsync();
        });
        _hubConnection.On<string, Card?>(IGameEvents.EventNames.CardPlayed, async (playerId, card) =>
        {
            if (playerId == _player?.Id)
            {
                await InvokeAsync(StateHasChanged);
            }
        });
        _hubConnection.On(IGameEvents.EventNames.RoundResultsCompleted, async () =>
        {
            _waitingForMonitor = true;
            await InvokeAsync(StateHasChanged);
        });
        _hubConnection.On(IGameEvents.EventNames.GameRestarted, async () =>
        {
            _player = null;
            await InvokeAsync(StateHasChanged);

        });
        _hubConnection.On(IGameEvents.EventNames.RoundCompleted, async () =>
        {
            //_waitingForMonitor = false;
            //await _player.SetSelectedCardAsync(null);
            //await SetAppearClassNameDelayed();
            await InvokeAsync(StateHasChanged);
        });

        _hubConnection.On<GameState>(IGameEvents.EventNames.RevealDone, async (gameStateWhenRevealed) =>
        {
            if (_player == null) return;
            _player.ResetCards();
            StartTimer();

            // TODO: _lastRevealDone is set too early
            if (gameStateWhenRevealed == GameState.Ended) _lastRevealDone = true;
            _waitingForMonitor = false;
            _showMenu = false;
            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync(IGameEvents.JoinGameMethod, _game!.Id);

        await InvokeAsync(StateHasChanged);
    }

    private const string PlayerIdKey = "player_id";
    private const string PlayerNameKey = "player_name";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var playerIdFromLocalStorage = await ProtectedLocalStorage.GetAsync<string>(PlayerIdKey);
        if (playerIdFromLocalStorage.Success) _playerId = playerIdFromLocalStorage.Value;
        if (string.IsNullOrWhiteSpace(_playerId))
        {
            _playerId = Guid.NewGuid().ToString();
            await ProtectedLocalStorage.SetAsync(PlayerIdKey, _playerId);
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", PlayerIdKey + "_plain", _playerId); // TODO: Remove
        }

        var playerNameFromLocalStorage = await ProtectedLocalStorage.GetAsync<string>(PlayerNameKey);
        if (playerNameFromLocalStorage.Success) _playerName = playerNameFromLocalStorage.Value;
        if (string.IsNullOrWhiteSpace(_playerName))
        {
            RandomizeNickname();
        }

        _initializing = false;
        await InvokeAsync(StateHasChanged);
    }

    private void StartTimer()
    {
        if (_game == null || _timer == null) return;
        if (_game.Rules.SelectCardTimeoutSeconds < 1) return;
        if (_game.State != GameState.Playing) return;

        _secondsLeft = _game.Rules.SelectCardTimeoutSeconds;
        _timer.Change(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
    }

    private void StopTimer()
    {
        _secondsLeft = 0;
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }


    private async Task JoinGameAsync()
    {
        if (_game == null || _playerId == null) return;

        _player = _game.Players.FirstOrDefault(x => x.Id == _playerId);
        if (_player == null)
        {
            var character = Character.Random(_game.AllCharacters());
            _player = new Player(_playerId, character);
            await AddPlayerAsync();
        }

        await InvokeAsync(StateHasChanged);
    }


    private async Task AddPlayerAsync()
    {
        if (_game == null || _player == null) return;
        if (_game.Players.Any(p => p.Id == _player.Id)) return;

        if (_game.State != GameState.Created)
        {
            _gameHasStartedAlready = true;
            return;
        }

        _playerNameInfo = null;
        if (!string.IsNullOrWhiteSpace(_playerName))
        {
            if (_game.Players.Any(p => string.Equals(p.Name, _playerName, StringComparison.OrdinalIgnoreCase)))
            {
                _playerNameInfo = "That nickname is taken.";
                return;
            }
            if (_playerName.Length > MaxPlayerNameLength) _playerName = _playerName[..MaxPlayerNameLength];
            _player.Name = _playerName;
            await ProtectedLocalStorage.SetAsync(PlayerNameKey, _playerName);
        }

        try
        {
            await _game.AddPlayerAsync(_player);
        }
        catch (ArgumentOutOfRangeException)
        {
            _gameIsFull = true;
        }
    }

    private async Task GameStateChangedAsync()
    {
        if (_game == null || _player == null) return;
        if (_game.State == GameState.Playing)
        {
            if (_firstTimerStarted == false)
            {
                _firstTimerStarted = true;
                StartTimer();
            }
        }
        else
        {
            _firstTimerStarted = false;
        }
        await InvokeAsync(StateHasChanged);
    }

    private bool IsSelected(Card card)
    {
        if (_player == null) return false;
        if (_player.SelectedCard == null) return false;
        if (_player.SelectedCard is AttackCard selectedAttackCard && card is AttackCard attackCard)
        {
            return selectedAttackCard.Target.Id == attackCard.Target.Id;
        }
        return card.Type == _player.SelectedCard.Type;
    }

    //private async Task SetAppearClassNameDelayed()
    //{
    //    _appearClassName = null;
    //    await InvokeAsync(StateHasChanged);
    //    await Task.Delay(0);
    //    _appearClassName = "appear";
    //    await InvokeAsync(StateHasChanged);
    //}

    private async Task CardClicked(Card? card)
    {
        if (_waitingForMonitor) return;
        if (_player == null) return;

        await _player.SetSelectedCardAsync(card);
        StopTimer();
    }

    //private async Task TryRejoin()
    //{
    //    _rejoinStatus = null;
    //    await InvokeAsync(StateHasChanged);
    //    await Task.Delay(200);

    //    if (_game?.Id == GameRepository.DeveloperGameId)
    //    {
    //        _game = GameRepository.GetGame(_game.Id);
    //    }

    //    if (_game == null)
    //    {
    //        _rejoinStatus = "The game is no longer available";
    //    }
    //    else if (_game.State != GameState.Created)
    //    {
    //        _rejoinStatus = "The game does not accept new players at the moment.";
    //    }
    //    else
    //    {
    //        await AddPlayerAsync();
    //        if (_gameIsFull)
    //        {
    //            _rejoinStatus = "The game is full, so can't rejoin";
    //        }
    //    }

    //    await InvokeAsync(StateHasChanged);
    //}

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();
        if (_timer != null) await _timer.DisposeAsync();
    }

    private const int MaxPlayerNameLength = 16;

    private static readonly string[] SuggestedNicknames =
    [
        "Frank Wolf",
        "Jefe Escorpion",
        "Bandito Diablo",
        "Rosa Ramirez",
        "El Lobo Gris",
        "Maria Delgado",
        "Tomas Vasquez",
        "Lucky Santiago",
        "Coyote Jim",
        "Dolores Mendoza",
        "Angel Herrera",
        "Pancho Reyes",
        "Salvatore Cruz",
        "Juanita Morales",
        "Esteban Ortega",
        "Rico Morales",
        "Catalina Torres",
        "Vicente Alvarez"
    ];

    private async Task MaybeQuit()
    {
        await _confirmDialog.ShowAsync(new ConfirmDialog.Question("Really quit the game?", "Yes", "No"), async quit =>
        {
            if (quit == true && _game != null && _player != null)
            {
                await _game.RemovePlayerAsync(_player);
                _player = null;
                _showMenu = false;
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task HideMenuAsync()
    {
        _showMenu = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleMenuAsync()
    {
        _showMenu = !_showMenu;
        await InvokeAsync(StateHasChanged);
    }

    private void RandomizeNickname()
    {
        _playerName = SuggestedNicknames[Random.Shared.Next(SuggestedNicknames.Length)];
        StateHasChanged();
    }
}
