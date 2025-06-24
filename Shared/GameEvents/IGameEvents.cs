using Shared.Cards;
using Shared.Model;

#pragma warning disable CA1034
namespace Shared.GameEvents;

public interface IGameEvents
{
    public static readonly string HubUrl = "/gameeventshub";
    public static readonly string JoinGameMethod = "JoinGameAsync";
    public static readonly string RevealDoneMethod = "RevealDoneAsync";

    public static class EventNames
    {
        public const string PlayerJoined = nameof(PlayerJoined);
        public const string PlayerLeft = nameof(PlayerLeft);
        public const string NewRound = nameof(NewRound);
        public const string GameStateChanged = nameof(GameStateChanged);
        public const string RoundResultsCompleted = nameof(RoundResultsCompleted);
        public const string RoundCompleted = nameof(RoundCompleted);
        public const string CardPlayed = nameof(CardPlayed);
        public const string GameRestarted = nameof(GameRestarted);
        public const string RevealDone = nameof(RevealDone);
    };

    Task PlayerJoinedAsync(string gameId, string playerId);
    Task PlayerLeftAsync(string gameId, string playerId);
    Task NewRoundAsync(string gameId);
    Task GameStateChangedAsync(string gameId, GameState newState);
    Task CardPlayedAsync(string gameId, string playerId, Card? card);
    Task RoundResultsCompletedAsync(string gameId);
    Task RoundCompletedAsync(string gameId, RoundResult lastRound);
    Task GameRestarted(string gameId);

    Task JoinGameAsync(string gameId);
    Task RevealDoneAsync(string gameId, GameState state);
}