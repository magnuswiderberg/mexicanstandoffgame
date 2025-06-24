using Common.Cards;
using Common.GameEvents;
using Common.Model;
using Microsoft.AspNetCore.SignalR;

namespace Blazor.GameEvents;

public class GameEventsHub(IHubContext<GameEventsHub> hubContext) : Hub, IGameEvents
{
    /// <summary>
    /// Used by clients to connect to the hub
    /// </summary>
    public async Task JoinGameAsync(string gameId)
    {
        await hubContext.Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    /// <summary>
    /// Used by game monitor to finish off a game
    /// </summary>
    public async Task RevealDoneAsync(string gameId, GameState state)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.RevealDone, state);
    }

    public async Task PlayerJoinedAsync(string gameId, string playerId)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.PlayerJoined, playerId);
    }

    public async Task PlayerLeftAsync(string gameId, string playerId)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.PlayerLeft, playerId);
    }

    public async Task NewRoundAsync(string gameId)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.NewRound);
    }

    public async Task GameStateChangedAsync(string gameId, GameState newState)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.GameStateChanged, newState);
    }

    public async Task RoundResultsCompletedAsync(string gameId)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.RoundResultsCompleted);
    }

    public async Task RoundCompletedAsync(string gameId, RoundResult lastRound)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.RoundCompleted);
    }

    public async Task GameRestarted(string gameId)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.GameRestarted);
    }

    public async Task CardPlayedAsync(string gameId, string playerId, Card? card)
    {
        await hubContext.Clients.Groups(gameId).SendAsync(IGameEvents.EventNames.CardPlayed, playerId, card);
    }
}