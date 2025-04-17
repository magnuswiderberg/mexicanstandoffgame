using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QRCoder;

namespace Blazor.Components.Pages.GameMonitor;

public partial class WaitForJoin : IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;


    private string? _gameUrl;
    private string? _qrCodeImageAsBase64;


    protected override void OnInitialized()
    {
        _gameUrl = $"{NavigationManager.BaseUri}play/{Game.Id}";

        using var qrEncoder = new QRCodeGenerator();
        var qrCodeData = qrEncoder.CreateQrCode(_gameUrl, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new Base64QRCode(qrCodeData);
        _qrCodeImageAsBase64 = qrCode.GetGraphic(20, Color.DarkOrange, Color.White);

        Game.PlayerJoined += PlayerCountChanged;
        Game.PlayerLeft += PlayerCountChanged;
        Game.GameStateChanged += GameStateChanged;

    }

    private void PlayerCountChanged(object? sender, EventArgs e)
    {
        PlayerCountChangedAction.Invoke();
    }

    private void GameStateChanged(object? sender, EventArgs e)
    {
        GameStateChangedAction.Invoke();
    }

    public void Dispose()
    {
        Game.PlayerJoined -= PlayerCountChanged;
        Game.PlayerLeft -= PlayerCountChanged;
        Game.GameStateChanged -= GameStateChanged;
    }

    private void StartGame()
    {
        Game.Start();
    }
}