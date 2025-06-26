using Common.BotPlay;
using Common.Cards;
using Game.Bots;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Controllers;

[Route("api/bots/mrtrigger")]
[ApiController]
public class MrTriggerBotController : ControllerBase
{
    [HttpGet("")]
    public BotInfo Info()
    {
        return new()
        {
            Name = "Mr. Trigger"
        };
    }

    [HttpPost("actions")]
    public Card Actions(GameContext gameContext)
    {
        var me = gameContext.Me;

        // Load if no bullets
        if (me.Bullets == 0) return Card.Load;

        var targets = gameContext.OtherPlayers.ToList();

        // Shoot player with most coins
        var maxCoins = targets.Select(x => x.Coins).Max();
        if (maxCoins > 0)
        {
            var coinMasters = targets.Where(x => x.Coins == maxCoins).ToList();
            var coinMaster = coinMasters[Random.Shared.Next(coinMasters.Count)];
            return new AttackCard(coinMaster.PlayerId);
        }

        // Other players do not have coins, just shoot someone
        var target = targets[Random.Shared.Next(targets.Count)];
        return new AttackCard(target.PlayerId);
    }

    [HttpPost("results")]
    public void Actions(PlayerRoundResult result)
    {
    }
}