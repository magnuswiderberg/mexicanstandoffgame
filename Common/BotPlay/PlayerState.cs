using Common.Cards;

namespace Common.BotPlay;

public class PlayerState
{
    public string Id { get; set; } = null!;
    public Character Character { get; set; } = null!;
    public string Name => Character.Name;
    public bool Alive { get; set; }
    public int Coins { get; set; }
    public int Shots { get; set; }
    public int Bullets { get; set; }
}