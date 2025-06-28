using System.Drawing;
using System.Net;
using Blazor.Components.Elements;
using Common.Cards;
using Common.Model;
using Game.Bots;
using Game.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QRCoder;

namespace Blazor.Components.Pages.GameMonitor;

public partial class WaitForJoin
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] public IBotService BotService { get; set; } = null!;


    private string? _gameUrl;
    private string? _qrCodeImageAsBase64;

    private ConfirmDialog _confirmDialog = null!;

    private InputDialog _externalBotDialog = null!;
    private string? _externalBotUrl;
    private BotInfo _externalBotInfo = new();
    private string? _externalBotError;
    private bool _externalBotFetchFailed;

    protected override void OnInitialized()
    {
        _gameUrl = $"{NavigationManager.BaseUri}play/{Game.Id}";

        using var qrEncoder = new QRCodeGenerator();
        var qrCodeData = qrEncoder.CreateQrCode(_gameUrl, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new Base64QRCode(qrCodeData);
        _qrCodeImageAsBase64 = qrCode.GetGraphic(20, Color.FromArgb(255, 64, 64, 64), Color.White);
    }

    private async Task StartGameAsync()
    {
        await Game.StartAsync();
    }

    private async Task ShowBotDialog(string saveText)
    {
        _externalBotUrl = null;
        _externalBotInfo = new();
        _externalBotError = null;
        _externalBotFetchFailed = false;
        await InvokeAsync(StateHasChanged);

        await _externalBotDialog.ShowAsync(new(saveText, SaveEnabled: false, "Cancel"), async add =>
        {
            if (add != true) return;
            if (_externalBotInfo.Name == null) return;
            if (!Uri.TryCreate(_externalBotUrl, UriKind.Absolute, out var botUri)) return;
            if (string.IsNullOrWhiteSpace(_externalBotInfo.Name))
            {
                _externalBotError = "Empty name";
                await InvokeAsync(StateHasChanged);
                return;
            }

            var botId = PlayerId.From(Guid.NewGuid().ToString());
            var addResult = await Game.AddPlayerAsync(new ApiBot(botUri, botId, _externalBotInfo.Name, Character.Random(Game.AllCharacters())));
            
            if (addResult != AddPlayerResultType.Success)
            {
                _externalBotError = addResult switch
                {
                    AddPlayerResultType.NameTaken => "Name already taken",
                    AddPlayerResultType.NoSeatsLeft => "No seats left",
                    _ => "Unknown error"
                };
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task LoadBotInfoAsync()
    {
        _externalBotError = null;
        _externalBotFetchFailed = false;
        if (!Uri.TryCreate(_externalBotUrl, UriKind.Absolute, out var botUri)) return;

        _externalBotInfo = await BotService.GetBotInfoAsync(botUri) ?? new();
        if (_externalBotInfo.Name != null) _externalBotDialog.SetSaveEnabled(true);
        else _externalBotFetchFailed = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task MaybeKickPLayer(Player player)
    {
        var question = $"Really kick {WebUtility.HtmlEncode(player.Name)} from the game?";
        await _confirmDialog.ShowAsync(new ConfirmDialog.Question(question, "Yes", "No", "!max-w-[300px]"), async remove =>
        {
            if (remove != true) return;
            await Game.RemovePlayerAsync(player);
        });
    }
}