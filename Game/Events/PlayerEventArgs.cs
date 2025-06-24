using Game.Model;

namespace Game.Events
{
    public class PlayerEventArgs(Player player) : EventArgs
    {
        public Player Player { get; } = player;
    }
}