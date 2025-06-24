using Shared.Cards;

namespace Shared.BotPlay
{
    public class GameContext
    {
        public string GameId { get; set; }
        public RuleSet Rules { get; set; }
        public int RoundNumber { get; set; }
        public List<Card> SelectableCards { get; set; }
        public List<PlayerState> Players { get; set; }
    }
}
