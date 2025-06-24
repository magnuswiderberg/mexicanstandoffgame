using System.Net;
using Blazor.Components.Elements;
using Common.GameEvents;
using Common.Model;
using Game.Bots;
using Game.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Blazor.Components.Pages.GameMonitor;

public partial class ShowGame
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public ProtectedLocalStorage ProtectedLocalStorage { get; set; } = null!;

    private List<AggregatedRoundAction> _aggregatedRoundResult = [];

    private int? _revealingRoundResultIndex;
    private string? _appearClassName;
    private ConfirmDialog _confirmDialog = null!;

    private int? _tutorialStep;


    private const string TutorialDoneKey = "gamemonitor_tutorial_done";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var tutorialDoneValueTask = await ProtectedLocalStorage.GetAsync<bool>(TutorialDoneKey);
        if (tutorialDoneValueTask.Success) _tutorialStep = tutorialDoneValueTask.Value ? null : 1;
        else _tutorialStep = 1;
        await InvokeAsync(StateHasChanged);
    }

    public async Task RoundResultsCompletedAsync()
    {
        foreach (var player in Game.AlivePlayers)
        {
            if (player is BotPlayer bot)
            {
                await bot.RoundCompleted(Game, Game.LastRound);
            }
        }

        //if (Game.Players.All(x => x is BotPlayer))
        //{
        //    await Game.SetRoundCompletedAsync();
        //}
        //else
        {
            _revealingRoundResultIndex = null;
            _aggregatedRoundResult = Game.CreateLastRoundAggregate();
            if (_aggregatedRoundResult.Count != 0)
            {
                _revealingRoundResultIndex = 0;
                await InvokeAsync(SetAppearClassNameDelayed);

                if (_revealingRoundResultIndex.HasValue)
                {
                    var result = _aggregatedRoundResult[_revealingRoundResultIndex.Value];
                    await PlayActionSoundAsync(result);
                }
            }
        }

        if (_tutorialStep == 6)
        {
            _tutorialStep = 7;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetAppearClassNameDelayed()
    {
        _appearClassName = null;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(300);
        _appearClassName = "appear";
        await InvokeAsync(StateHasChanged);
    }

    private async Task RevealNextAsync()
    {
        _revealingRoundResultIndex++;
        if (_revealingRoundResultIndex >= _aggregatedRoundResult.Count)
        {
            _revealingRoundResultIndex = null;
            _appearClassName = null;
            _aggregatedRoundResult.Clear();

            await Game.SetRoundCompletedAsync();
            await HubConnection.InvokeAsync(IGameEvents.RevealDoneMethod, Game.Id, Game.State);
            await EndTutorial(0);
        }
        else if (_revealingRoundResultIndex.HasValue)
        {
            var result = _aggregatedRoundResult[_revealingRoundResultIndex.Value];
            await PlayActionSoundAsync(result);
        }

        await InvokeAsync(SetAppearClassNameDelayed);
        await InvokeAsync(StateHasChanged);
    }

    private MarkupString RevealNextText()
    {
        if (_revealingRoundResultIndex == null) return (MarkupString)"";
        if (_revealingRoundResultIndex >= _aggregatedRoundResult.Count - 1) return (MarkupString)"That's all. Move on&hellip;";
        return (MarkupString)"Reveal next &gt;";
    }

    private async Task PlayActionSoundAsync(AggregatedRoundAction? result)
    {
        if (result == null) return;

        for (var i = 0; i < result.Attackers?.Count; i++)
        {
            await JsRuntime.InvokeVoidAsync("playSound", "sound-shot");
            await Task.Delay(200);
        }

        if (result.Type == RoundActionType.Dodge)
        {
            for (var i = 0; i < result.TargetPlayers.Count; i++)
            {
                await JsRuntime.InvokeVoidAsync("playSound", "sound-dodge");
                await Task.Delay(200);
            }
        }
        else if (result.Type == RoundActionType.Attack)
        {
            if (result.Successful)
            {
                await Task.Delay(400);
                await JsRuntime.InvokeVoidAsync("playSound", "sound-grunt");
            }
        }
        else if (result.Type == RoundActionType.Chest)
        {
            for (var i = 0; i < result.TargetPlayers.Count; i++)
            {
                if (result.Successful)
                {
                    await JsRuntime.InvokeVoidAsync("playSound", "sound-coin");
                }
                else
                {
                    await JsRuntime.InvokeVoidAsync("playSound", "sound-missed-chest");
                }
                await Task.Delay(200);
            }
        }
        else if (result.Type == RoundActionType.Load)
        {
            for (var i = 0; i < result.TargetPlayers.Count; i++)
            {
                await JsRuntime.InvokeVoidAsync("playSound", "sound-load");
                await Task.Delay(300);
            }
        }
    }

    private async Task RemovePlayerAsync(Player player)
    {
        var question = $"Really kick {WebUtility.HtmlEncode(player.Name)} from the game?";
        await _confirmDialog.ShowAsync(new ConfirmDialog.Question(question, "Yes", "No"), async remove =>
        {
            if (remove != true) return;
            await Game.RemovePlayerAsync(player);
        });
    }

    private async Task ChangeTutorialStep(int nextStep)
    {
        _tutorialStep = nextStep;
        await InvokeAsync(StateHasChanged);
    }

    private async Task EndTutorial(int _)
    {
        await ProtectedLocalStorage.SetAsync(TutorialDoneKey, true);
        _tutorialStep = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ActivateTutorial()
    {
        await ProtectedLocalStorage.DeleteAsync(TutorialDoneKey);
        _tutorialStep = 1;
        await InvokeAsync(StateHasChanged);
    }
}