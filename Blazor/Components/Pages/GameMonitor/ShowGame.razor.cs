using Game.Bots;
using Game.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazor.Components.Pages.GameMonitor;

public partial class ShowGame : IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    private List<RoundAction> _lastRoundResult = new();

    //private bool _animationDone;
    //protected bool AnimationDone
    //{
    //    get => _animationDone;
    //    set
    //    {
    //        _animationDone = value;
    //        if (_animationDone) Game.SetRoundCompleted();
    //    }
    //}

    private int? _revealingRoundResultIndex;
    private string? _appearClassName;

    protected override void OnInitialized()
    {
        Game.GameStateChanged += GameStateChanged;
        Game.RoundResultsCompleted += RoundResultsCompleted;
        foreach (var player in Game.Players)
        {
            player.CardChanged += GameStateChanged;
        }
    }

    private void GameStateChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
        GameStateChangedAction.Invoke();
    }


    private void RoundResultsCompleted(object? sender, EventArgs e)
    {

        if (Game.Players.All(x => x is BotPlayer))
        {
            Game.SetRoundCompleted();
        }
        else
        {
            _revealingRoundResultIndex = null;
            _lastRoundResult.Clear();
            _lastRoundResult.AddRange(Game.LastRound.Actions);
            _revealingRoundResultIndex = 0;
            InvokeAsync(SetAppearClassNameDelayed);
        }

        //AnimationDone = false;
        InvokeAsync(StateHasChanged);
        // TODO InvokeAsync(() => JsRuntime.InvokeVoidAsync("showResults")).Wait();
    }

    private async Task SetAppearClassNameDelayed()
    {
        _appearClassName = null;
        StateHasChanged();
        await Task.Delay(300);
        _appearClassName = "appear";
        StateHasChanged();
    }

    public void Dispose()
    {
        Game.GameStateChanged -= GameStateChanged;
        Game.RoundResultsCompleted -= RoundResultsCompleted;
        foreach (var player in Game.Players)
        {
            player.CardChanged -= GameStateChanged;
        }
    }

    private void RevealNext()
    {
        if (++_revealingRoundResultIndex >= _lastRoundResult.Count)
        {
            _revealingRoundResultIndex = null;
            _appearClassName = null;
            _lastRoundResult.Clear();
            Game.SetRoundCompleted();
        }

        InvokeAsync(SetAppearClassNameDelayed);
        InvokeAsync(StateHasChanged);
    }

    private MarkupString RevealNextText()
    {
        if (_revealingRoundResultIndex == null) return (MarkupString)"";
        if (_revealingRoundResultIndex >= _lastRoundResult.Count - 1) return (MarkupString)"That's all. Move on&hellip;";
        return (MarkupString)"Reveal next &gt;";
    }

    private void RemovePlayer(Player player)
    {
        Game.RemovePlayer(player);
    }
}