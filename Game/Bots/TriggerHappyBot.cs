using Shared.BotPlay;
using Shared.Cards;

namespace Game.Bots;

[Obsolete("Bot play is now through the API")]
public class TriggerHappyBot(string id, Character character) : BotPlayer(id, character)
{
    public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
    {
        await Task.Yield();

        // Load if no bullets
        if (Bullets == 0) return Card.Load;

        var targets = game.Players.Where(p => p.Id != Id).ToList();

        // Shoot player with most coins
        var maxCoins = game.Players.Select(x => x.Coins).Max();
        if (maxCoins > 0)
        {
            var coinMasters = targets.Where(x => x.Coins == maxCoins).ToList();
            var coinMaster = coinMasters[Random.Shared.Next(coinMasters.Count)];
            return new AttackCard(coinMaster.Character);
        }

        // Other players do not have coins, just shoot someone
        var target = targets[Random.Shared.Next(targets.Count)];
        return new AttackCard(target.Character);
    }

    public override Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
    {
        return Task.CompletedTask;
    }
}