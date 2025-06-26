using Common.Cards;
using Common.Model;

namespace Common.BotPlay;

public class PlayerRoundResult
{
    public string GameId { get; set; } = null!;
    public GameState GameState { get; set; }
    public int Round { get; set; }
    public PlayerRoundResultAction Action { get; set; } = null!;
    public IReadOnlyList<PlayerRoundResultOtherPlayer> OtherPlayers { get; set; } = null!;

}

public class PlayerRoundResultAction
{
    public bool Success { get; set; }
    public PlayerRoundResultCard Card { get; set; } = null!;
}

public class PlayerRoundResultCard
{
    public string Type { get; set; } = null!;
    public string? Target { get; set; }
}

public class PlayerRoundResultOtherPlayer : PlayerRoundResultAction
{
    public string PlayerId { get; set; } = null!;
}