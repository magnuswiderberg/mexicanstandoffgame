using Shared.Cards;

namespace Game.Model;

public enum RoundActionType
{
    Dodge,
    Load,
    Chest,
    Attack,
}

public class RoundAction(RoundActionType type, Character source, bool success)
{
    public RoundActionType Type { get; } = type;
    public Character Source { get; set; } = source;
    public bool Success { get; } = success;

    public Character? Target { get; set; }
    public bool Shot { get; set; }
    //public int ChestNumber { get; set; }
}