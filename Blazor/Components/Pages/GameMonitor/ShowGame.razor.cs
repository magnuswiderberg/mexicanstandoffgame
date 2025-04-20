using Game.Bots;
using Game.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Shared.GameEvents;
using Shared.Model;

namespace Blazor.Components.Pages.GameMonitor;

public partial class ShowGame
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    private List<AggregatedRoundAction> _aggregatedRoundResult = new();

    private int? _revealingRoundResultIndex;
    private string? _appearClassName;


    public async Task RoundResultsCompletedAsync()
    {
        foreach (var player in Game.AlivePlayers)
        {
            if (player is BotPlayer bot)
            {
                await bot.RoundCompleted(Game, Game.LastRound);
            }
        }

        if (Game.Players.All(x => x is BotPlayer))
        {
            await Game.SetRoundCompletedAsync();
        }
        else
        {
            _revealingRoundResultIndex = null;
            _aggregatedRoundResult = Game.CreateLastRoundAggregate();
            _revealingRoundResultIndex = 0;
            await InvokeAsync(SetAppearClassNameDelayed);

            if (_revealingRoundResultIndex.HasValue)
            {
                var result = _aggregatedRoundResult[_revealingRoundResultIndex.Value];
                await PlayActionSoundAsync(result);
            }
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
                    await JsRuntime.InvokeVoidAsync("playSound", "sound-chest");
                }
                else
                {
                    await JsRuntime.InvokeVoidAsync("playSound", "sound-missed-chest");
                }
                await JsRuntime.InvokeVoidAsync("playSound", "sound-coin");
                await Task.Delay(200);
            }
        }
        else if (result.Type == RoundActionType.Load)
        {
            for (var i = 0; i < result.TargetPlayers.Count; i++)
            {
                await JsRuntime.InvokeVoidAsync("playSound", "sound-load");
                await Task.Delay(200);
            }
        }
    }

    private async Task RemovePlayerAsync(Player player)
    {
        await Game.RemovePlayerAsync(player);
    }
}