using Game.Model;
using Microsoft.AspNetCore.Components;
using Game.Repository;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Shared.Cards;
using Microsoft.JSInterop;

namespace Blazor.Components.Pages;

public partial class Play
{
    [Inject] public GameRepository GameRepository { get; set; } = null!;
    [Inject] public ProtectedLocalStorage ProtectedLocalStorage { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    private Game.Logic.Game? _game;
    private Player? _player;
    private Card? _selectedCard;
    private bool _gameIsFull;
    private bool _waitingForAnimation;
    private string? _appearClassName;
    private string? _rejoinStatus;

    private Card? SelectedCard
    {
        get => _selectedCard;
        set
        {
            // We want user to see what happens
            InvokeAsync(() => Task.Delay(TimeSpan.FromMilliseconds(300)));
            _selectedCard = value;
            _player?.SetSelectedCard(_selectedCard);
        }
    }

    protected override void OnInitialized()
    {
        _game = GameRepository.GetGame(GameId);
        if (_game == null) return;

        _game.PlayerJoined += GameStateChanged;
        _game.PlayerLeft += GameStateChanged;
        _game.GameStateChanged += GameStateChanged;
        _game.RoundResultsCompleted += RoundResultsCompleted;
        _game.RoundCompleted += NewRound;

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        if (_game == null) return;

        var key = CreatePlayerIdKey();
        string? playerId = null;
        var playerIdFromLocalStorage = await ProtectedLocalStorage.GetAsync<string>(key);
        if (playerIdFromLocalStorage.Success) playerId = playerIdFromLocalStorage.Value;
        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = Guid.NewGuid().ToString();
            await ProtectedLocalStorage.SetAsync(key, playerId);
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", key + "_plain", playerId);
        }

        _player = _game.Players.FirstOrDefault(x => x.Id == playerId);
        if (_player == null)
        {
            var character = Character.Random(_game.AllCharacters());
            _player = new Player(playerId, character);
            AddPlayer();
        }
        StateHasChanged(); // Note: needed because razor page can think _player is null otherwise
    }

    private void RoundResultsCompleted(object? sender, EventArgs e)
    {
        _waitingForAnimation = true;
    }

    private void NewRound(object? sender, EventArgs e)
    {
        if (_player == null) return;
        SelectedCard = null;
        _waitingForAnimation = false;
        _player.ResetCards();
        // TODO InvokeAsync(() => JsRuntime.InvokeVoidAsync("resetCards")).Wait();
        // TODO StartTimer();
        InvokeAsync(SetAppearClassNameDelayed);
        InvokeAsync(StateHasChanged);
    }

    private void AddPlayer()
    {
        if (_game == null || _player == null) return;
        if (_game.Players.Any(p => p.Id == _player.Id)) return;
        try
        {
            _game.AddPlayer(_player);
            Console.WriteLine($"Added player {_player.Id}, {_player.Character.Name}");
        }
        catch (ArgumentOutOfRangeException)
        {
            _gameIsFull = true;
        }
    }

    private void GameStateChanged(object? sender, EventArgs e)
    {
        if (_game == null || _player == null) return;
        // TODO: needed, no? AddPlayer(); // Re-add player if state is lost somehow
        if (_game.State == GameState.Created)
        {
            AddPlayer();
        }
        InvokeAsync(StateHasChanged);
    }

    private string CreatePlayerIdKey()
    {
        return $"player_id";
        //return $"player_id_{_game?.Id}";
    }

    private void Quit()
    {
        if (_game == null || _player == null) return;

        _game.RemovePlayer(_player);
        _game.PlayerJoined -= GameStateChanged;
        _game.PlayerLeft -= GameStateChanged;
        // Can't be done at this time: await ProtectedLocalStorage.DeleteAsync(CreatePlayerIdKey());
    }

    //public void Dispose()
    //{
    //    Console.WriteLine("Play dispose()");
    //    if (_game == null) return;
    //    InvokeAsync(QuitAsync);
    //}

    private static bool IsSelected(Card card, Card? selectedCard)
    {
        if (selectedCard == null) return false;
        if (selectedCard is AttackCard selectedAttackCard && card is AttackCard attackCard)
        {
            return selectedAttackCard.Target.Id == attackCard.Target.Id;
        }
        return card.Type == selectedCard.Type;
    }

    private async Task SetAppearClassNameDelayed()
    {
        _appearClassName = null;
        StateHasChanged();
        await Task.Delay(300);
        _appearClassName = "appear";
        StateHasChanged();
    }

    private void CardClicked(Card? card)
    {
        if (_waitingForAnimation) return;

        SelectedCard = card;
    }

    private async Task TryRejoin()
    {
        _rejoinStatus = null;
        StateHasChanged();
        await Task.Delay(200);

        if (_game?.Id == GameRepository.DeveloperGameId)
        {
            _game = GameRepository.GetGame(_game.Id);
        }

        if (_game == null)
        {
            _rejoinStatus = "The game is no longer available";
        }
        else if (_game.State != GameState.Created)
        {
            _rejoinStatus = "The game does not accept new players at the moment.";
        }
        else
        {
            AddPlayer();
            if (_gameIsFull)
            {
                _rejoinStatus = "The game is full, so can't rejoin";
            }
        }

        StateHasChanged();
    }
}