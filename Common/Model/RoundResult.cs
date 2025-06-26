namespace Common.Model;

public class RoundResult
{
    public IReadOnlyList<RoundAction> Actions { get; set; } = [];
    
    public List<RoundResultError> Errors { get; } = [];
}

public record RoundResultError(PlayerId PlayerId, string Error);
