namespace Game.Model;

public class RoundResultEventArgs : EventArgs
{
    public IReadOnlyList<RoundAction> Actions { get; set; } = null!;
    public List<string> Errors { get; } = [];
}