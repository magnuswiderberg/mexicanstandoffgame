using Game.Model;

namespace Game.Events
{
    public class PlayerEvent : EventArgs
    {
        public Player Player { get; }

        public PlayerEvent(Player player)
        {
            Player = player;
        }
    }
}