namespace Shared.Model;

public class RoundResult
{
    public IReadOnlyList<RoundAction> Actions { get; set; } = null!;
    public List<string> Errors { get; } = [];
}