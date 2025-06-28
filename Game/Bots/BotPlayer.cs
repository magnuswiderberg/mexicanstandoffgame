using Game.Model;
using Common.BotPlay;
using Common.Cards;
using Common.Model;

namespace Game.Bots;

public abstract class BotPlayer : Player
{
    protected BotPlayer(PlayerId id, string name, Character character) : base(id, character)
    {
        Name = name;
    }

    public abstract Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game);
    public abstract Task RoundResult(PlayerRoundResult roundResult, Logic.Game game);

    public async Task NewGameStateAsync(Logic.Game game)
    {
        await Task.Delay(200); // Note! This is vital in bot play
        await SelectCard(game);
    }

    private async Task SelectCard(Logic.Game game)
    {
        var selectableCards = game.PlayableCards(this);
        var selected = await ChooseCard(selectableCards, game);
        await SetSelectedCardAsync(selected);
    }

    public async Task RoundCompleted(Logic.Game game, RoundResult roundResult)
    {
        var playerAction = roundResult.Actions.FirstOrDefault(x => x.Source == Id);
        var playerResult = new PlayerRoundResult
        {
            GameId = game.Id,
            Round = game.Rounds,
            Action = new()
            {
                Success = playerAction?.Success ?? false,
                Card = new PlayerRoundResultCard
                {
                    Type = playerAction?.Type.ToString() ?? "Unknown",
                    Target = playerAction?.Target?.Value ?? null,
                }
            },
            GameState = game.State,
            OtherPlayers = roundResult.Actions
                .Where(x => x.Source != Id)
                .Select(x => new PlayerRoundResultOtherPlayer
                {
                    PlayerId = x.Source.Value,
                    Success = x.Success,
                    Card = new PlayerRoundResultCard
                    {
                        Type = x.Type.ToString(),
                        Target = x.Target?.Value ?? null,
                    }
                })
                .ToList()
        };
        await RoundResult(playerResult, game);
    }
}