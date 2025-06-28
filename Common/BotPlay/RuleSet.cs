namespace Common.BotPlay
{
    public class RuleSet
    {
        public int CoinsToWin { get; set; }
        public int ShotsToDie { get; set; }
        public int MaxBullets { get; set; }
        public IReadOnlyDictionary<int, int> ChestsPerPlayerCount { get; set; } = null!;
    }
}
