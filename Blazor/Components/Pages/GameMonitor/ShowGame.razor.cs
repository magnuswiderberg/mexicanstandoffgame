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
    private bool _revealButtonDisabled;
    private bool _lastRevealDone;
    
    private bool _useSpeech = true;

    private const string TutorialDoneKey = "gamemonitor_tutorial_done";
    private const string UseSpeechKey = "use_speech";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var tutorialDoneValueTask = await ProtectedLocalStorage.GetAsync<bool>(TutorialDoneKey);
        if (tutorialDoneValueTask.Success) _tutorialStep = tutorialDoneValueTask.Value ? null : 1;
        else _tutorialStep = 1;

        var useSpeechValueTask = await ProtectedLocalStorage.GetAsync<bool>(UseSpeechKey);
        if (useSpeechValueTask.Success) _useSpeech = useSpeechValueTask.Value;
        
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

        _revealButtonDisabled = true;
        try
        {
            _revealingRoundResultIndex = null;
            _aggregatedRoundResult = Game.CreateLastRoundAggregate();
            if (_aggregatedRoundResult.Count != 0)
            {
                _revealingRoundResultIndex = 0;
                await InvokeAsync(SetAppearClassNameDelayed);
                await InvokeAsync(StateHasChanged);

                if (_revealingRoundResultIndex.HasValue)
                {
                    var result = _aggregatedRoundResult[_revealingRoundResultIndex.Value];
                    await PlayActionSoundAsync(result);
                }
            }
        }
        finally
        {
            _revealButtonDisabled = false;
            if (_useSpeech) await RevealNextAsync();
            if (Game.State == GameState.Ended)
            {
                _lastRevealDone = true;
            }

            if (_tutorialStep == 6)
            {
                _tutorialStep = 7;
            }

            await InvokeAsync(StateHasChanged);
        }
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
        _revealButtonDisabled = true;
        try
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
                await InvokeAsync(StateHasChanged);
                var result = _aggregatedRoundResult[_revealingRoundResultIndex.Value];
                await PlayActionSoundAsync(result);
            }

            await InvokeAsync(SetAppearClassNameDelayed);
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _revealButtonDisabled = false;
            if (_revealingRoundResultIndex < _aggregatedRoundResult.Count)
            {
                if (_useSpeech) await RevealNextAsync();
            }
            else if (Game.State == GameState.Ended)
            {
                _lastRevealDone = true;
            }
        }
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
                    // TODO: does not play well with the speech; await JsRuntime.InvokeVoidAsync("playSound", "sound-missed-chest");
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

        await SpeakActionsAsync(result);
    }

    private async Task SpeakActionsAsync(AggregatedRoundAction result)
    {
        if (_useSpeech == false) return;

        if (result.Attackers?.Count == 1)
        {
            var text = $"{result.Attackers[0].Name} attacks {result.TargetPlayers[0].Name} ...";
            if (result.Successful && result.TargetPlayers[0].Alive == false) text += " who dies";
            else if (result.Successful) text += " and hits!";
            else text += " but misses!";
            await JsRuntime.InvokeVoidAsync("Speak", text);
        }
        else if (result.Attackers?.Count > 1)
        {
            var attackers = string.Join(" and ", result.Attackers.Select(p => p.Name));
            var text = $"{attackers} attack {result.TargetPlayers[0].Name} ...";
            if (result.Successful && result.TargetPlayers[0].Alive == false) text += " who dies"; // TODO: Gets coins...
            else if (result.Successful) text += " and hits!";
            else text += " but misses!";
            await JsRuntime.InvokeVoidAsync("Speak", text);
        }

        if (result.Type == RoundActionType.Dodge)
        {
            if (result.TargetPlayers.Count == 1)
            {
                var text = $"{result.TargetPlayers[0].Name} dodges ...";
                if (result.Attackers == null) text += " in vain!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
            else
            {
                var extraWord = result.TargetPlayers.Count > 2 ? "all" : "both";
                var text = $"{string.Join(" and ", result.TargetPlayers.Select(p => p.Name))} {extraWord} dodge ...";
                if (result.Attackers == null) text += " in vain!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
        }
        else if (result.Type == RoundActionType.Chest)
        {
            if (result.TargetPlayers.Count == 1)
            {
                var text = $"{result.TargetPlayers[0].Name} goes to the chest ...";
                if (result.Successful) text += " and gets a coin!";
                else text += " in vain!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
            else
            {
                var extraWord = result.TargetPlayers.Count > 2 ? "all" : "both";
                var text = $"{string.Join(" and ", result.TargetPlayers.Select(p => p.Name))} {extraWord} go to the chest ...";
                if (result.Successful) text += " and get a coin each!";
                else text += " in vain!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
        }
        else if (result.Type == RoundActionType.Load)
        {
            if (result.TargetPlayers.Count == 1)
            {
                var text = $"{result.TargetPlayers[0].Name} loads the gun ...";
                if (result.Successful == false) text += " but fails!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
            else
            {
                var extraWord = result.TargetPlayers.Count > 2 ? "all" : "both";
                var text = $"{string.Join(" and ", result.TargetPlayers.Select(p => p.Name))} {extraWord} load their guns ...";
                if (result.Successful == false) text += " but fails!";
                await JsRuntime.InvokeVoidAsync("Speak", text);
            }
        }
    }

    private async Task RemovePlayerAsync(Player player)
    {
        var question = $"Really kick {WebUtility.HtmlEncode(player.Name)} from the game?";
        await _confirmDialog.ShowAsync(new ConfirmDialog.Question(question, "Yes", "No", "!max-w-[300px]"), async remove =>
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

    private async Task MaybeQuitAsync()
    {
        const string question = "Really QUIT the game?";
        await _confirmDialog.ShowAsync(new ConfirmDialog.Question(question, "Yes", "No", "!max-w-[300px]"), async quit =>
        {
            if (quit != true) return;
            await Game.AbortAsync();
            NavigationManager.NavigateTo("/");
        });
    }

    public async Task WaitForLastRevealAsync()
    {
        if (_lastRevealDone) return;

        var maxWaitUntil = DateTime.UtcNow.AddSeconds(15);
        while (_lastRevealDone == false)
        {
            await Task.Delay(100);
            if (DateTime.UtcNow > maxWaitUntil) break;
        }
    }

    private async Task UseSpeechToggled()
    {
        await ProtectedLocalStorage.SetAsync(UseSpeechKey, _useSpeech);
    }

    public bool UseSpeech()
    {
        return _useSpeech;
    }
}