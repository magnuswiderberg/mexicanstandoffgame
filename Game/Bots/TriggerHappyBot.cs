//using Shared.BotPlay;
//using Shared.Cards;

//namespace Game.Bots;

//[Obsolete("Bot play is now through the API")]
//public class TriggerHappyBot(string id, Character character) : BotPlayer(id, character)
//{
//    private static readonly Random Random = new Random();

//    public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
//    {
//        await Task.Yield();

//        // Load if no bullets
//        if (Bullets == 0) return Card.Load;

//        // Shoot player with most coins
//        var maxCoins = game.Players.Select(x => x.Coins).Max();
//        if (maxCoins > 0)
//        {
//            var coinMasters = game.Players.Where(x => x.Coins == maxCoins).ToList();
//            var target = coinMasters[Random.Next(coinMasters.Count)];
//            return new AttackCard(target.Character);
//        }

//        // Other players does not have coins, just do something
//        var options = new List<Card>();
//        if (Coins > 0) options.Add(Card.Dodge);
//        if (Bullets < game.Rules.MaxBullets) options.Add(Card.Load);
//        return options[Random.Next(options.Count)];
//    }

//    public override Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
//    {
//        return Task.CompletedTask;
//    }
//}