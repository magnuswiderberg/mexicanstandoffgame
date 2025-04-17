using Game.Model;

namespace Game.Events
{
    public class PlayerJoinedEvent : PlayerEvent
    {
        public PlayerJoinedEvent(Player player) : base(player) { }
    }
}