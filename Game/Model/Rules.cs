namespace Game.Model;

public class Rules
{
    public int MinimumPlayerCount { get; set; } = 3;
    public int MaximumPlayerCount { get; set; } = 8;

    public int CoinsToWin { get; set; } = 3;
    public int ShotsToDie { get; set; } = 2;
    public int MaxBullets { get; set; } = 2;

    public int SelectCardTimeoutSeconds { get; set; }

    public IReadOnlyDictionary<int, int> ChestsPerPlayerCount { get; set; } = new Dictionary<int, int>
    {
        { 0, 0 }, // Needed for when access with Game.Players.Count == 0
        { 1, 1 },
        { 2, 1 },
        { 3, 1 },
        { 4, 2 },
        { 5, 2 },
        { 6, 2 },
        { 7, 3 },
        { 8, 3 }
    };
}