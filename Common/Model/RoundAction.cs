namespace Common.Model;

public enum RoundActionType
{
    Dodge,
    Load,
    Chest,
    Attack,
    Error,
}

public class RoundAction(RoundActionType type, PlayerId source, bool success)
{
    public RoundActionType Type { get; } = type;
    public PlayerId Source { get; set; } = source;
    public bool Success { get; } = success;

    public PlayerId? Target { get; set; }
    public bool Shot { get; set; }
}