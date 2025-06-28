using Common.BotPlay;
using Common.Cards;
using Game.Bots;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Controllers;

[Route("api/bots/mrgold")]
[ApiController]
public class MrGoldBotController : ControllerBase
{
    [HttpGet("")]
    public BotInfo Info()
    {
        return new()
        {
            Name = "Mr. Gold"
        };
    }

    [HttpPost("actions")]
    public Card Actions(GameContext gameContext)
    {
        var me = gameContext.Me;

        // Sometimes we should dodge
        var attackers = gameContext.OtherPlayers
            .Where(x => x is { Bullets: > 0, Coins: > 0 })
            .ToList();
        if (attackers.Count != 0 &&
            me.Coins > 0
            && Random.Shared.NextDouble() < 0.25)
        {
            return Card.Dodge;
        }

        // Go for a chest
        return Card.Chest;
    }

    [HttpPost("results")]
    public void Actions(PlayerRoundResult result)
    {
    }
}