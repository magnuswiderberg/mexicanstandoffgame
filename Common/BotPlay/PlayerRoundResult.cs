namespace Common.BotPlay;

public class PlayerRoundResult
{
    public string GameId { get; set; } = null!;
    public int Round { get; set; }
    public string PlayerId { get; set; } = null!;
    public bool Success { get; set; }
}