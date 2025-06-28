using Common.Cards;

namespace Common.BotPlay;

public class GameContext
{
    public string GameId { get; set; } = null!;
    public RuleSet Rules { get; set; } = null!;
    public int RoundNumber { get; set; }
    public IReadOnlyList<Card> SelectableCards { get; set; } = null!;
    public PlayerState Me { get; set; } = null!;
    public IReadOnlyList<PlayerState> OtherPlayers { get; set; } = null!;
}