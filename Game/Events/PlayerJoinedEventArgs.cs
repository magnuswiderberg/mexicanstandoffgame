using Game.Model;

namespace Game.Events
{
    public class PlayerJoinedEventArgs(Player player) : PlayerEventArgs(player);
}