//using Shared.BotPlay;
//using Shared.Cards;

//namespace Game.Bots;

//[Obsolete("Bot play is now through the API")]
//public class CoinEagerBot(string id, Character character) : BotPlayer(id, character)
//{
//    private static readonly Random Random = new Random();

//    public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
//    {
//        await Task.Yield();

//        // Sometimes we should dodge
//        if (Coins > 0 && Random.NextDouble() < 0.25) return Card.Dodge;

//        // Go for a chest
//        var chests = selectableCards.Where(x => x is ChestCard).ToList();
//        return chests[Random.Next(chests.Count)];
//    }

//    public override Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
//    {
//        return Task.CompletedTask;
//    }
//}