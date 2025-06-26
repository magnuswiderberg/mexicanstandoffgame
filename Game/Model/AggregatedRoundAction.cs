using Common.Model;
#pragma warning disable CA2227

namespace Game.Model;

public class AggregatedRoundAction
{
    public List<Player> TargetPlayers { get; set; } = null!;
    public RoundActionType Type { get; set; }
    public bool Successful { get; set; }
    public List<Player>? Attackers { get; set; }
    
    public string? Error { get; set; }
}