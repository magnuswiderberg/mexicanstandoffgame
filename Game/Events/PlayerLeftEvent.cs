using Game.Model;

namespace Game.Events
{
    public class PlayerLeftEvent : PlayerEvent
    {
        public PlayerLeftEvent(Player player) : base(player) { }
    }
}