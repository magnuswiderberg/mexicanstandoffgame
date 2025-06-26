namespace Common.BotPlay;

public class PlayerState
{
    public string PlayerId { get; set; } = null!;
    public bool Alive { get; set; }
    public int Coins { get; set; }
    public int Shots { get; set; }
    public int Bullets { get; set; }
}