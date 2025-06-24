using Shared.Cards;

namespace Shared.BotPlay
{
    public class PlayerState
    {
        public string Id { get; set; }
        public Character Character { get; set; }
        public string Name => Character.Name;
        public bool Alive { get; set; }
        public int Coins { get; set; }
        public int Shots { get; set; }
        public int Bullets { get; set; }
    }
}