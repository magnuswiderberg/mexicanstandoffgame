using Game.Model;

namespace Game.Events
{
    public class PlayerLeftEventArgs(Player player) : PlayerEventArgs(player);
}