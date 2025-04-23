using Game.Model;
using Shared.BotPlay;
using Shared.Cards;
using Shared.Model;

namespace Game.Bots;

public abstract class BotPlayer : Player
{
    protected BotPlayer(string id, Character character) : base(id, character)
    {
        Name = id;
    }

    public abstract Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game);
    public abstract Task RoundResult(PlayerRoundResult roundResult, Logic.Game game);

    //public async Task NewGameState(object? sender, EventArgs e)
    public async Task NewGameStateAsync(Logic.Game game)
    {
        //if (sender is Logic.Game { State: GameState.Playing } game)
        {
            await Task.Delay(200); // Note! This is vital in bot play
            await SelectCard(game);
        }
    }

    private async Task SelectCard(Logic.Game game)
    {
        var selectableCards = game.PlayableCards(this);
        var selected = await ChooseCard(selectableCards, game);
        await SetSelectedCardAsync(selected);
    }

    //public async Task RoundCompleted(object? sender, EventArgs e)
    public async Task RoundCompleted(Logic.Game game, RoundResult roundResult)
    {
        //if (sender is not Logic.Game game) return;
        //var roundResult = (RoundResultEventArgs) e;
        var playerAction = roundResult.Actions.FirstOrDefault(x => x.Source.Id == Character.Id);
        var playerResult = new PlayerRoundResult
        {
            GameId = game.Id,
            PlayerId = Id,
            Round = game.Rounds,
            Success = playerAction?.Success ?? false
        };
        await RoundResult(playerResult, game);

        // TODO: Check if this is needed
        //base.NewRound();
        //await SelectCard(game);
    }
}