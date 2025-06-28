using Common.BotPlay;
using Common.Cards;
using Game.Bots;
using Microsoft.AspNetCore.Mvc;

namespace Blazor.Controllers;

[Route("api/bots/mrrandom")]
[ApiController]
public class MrRandomBotController : ControllerBase
{
    [HttpGet("")]
    public BotInfo Info()
    {
        return new()
        {
            Name = "Mr. Random"
        };
    }

    [HttpPost("actions")]
    public Card Actions(GameContext gameContext)
    {
        var card = gameContext.SelectableCards[Random.Shared.Next(gameContext.SelectableCards.Count)];
        if (card is AttackCard attackCard)
        {

        }
        return card;
    }

    [HttpPost("results")]
    public void Actions(PlayerRoundResult result)
    {
    }
}