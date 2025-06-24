using Shared.BotPlay;
using Shared.Cards;

namespace Game.Bots;

[Obsolete("Bot play is now through the API")]
public class RandomBot(string id, Character character) : BotPlayer(id, character)
{
    public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
    {
        await Task.Delay(500);

        return selectableCards[Random.Shared.Next(selectableCards.Count)];
    }

    public override Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
    {
        return Task.CompletedTask;
    }
}