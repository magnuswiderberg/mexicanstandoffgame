using Game.Model;
using Shared.BotPlay;
using Shared.Cards;

namespace Game.Bots;

public abstract class BotPlayer(string id, Character character) : Player(id, character)
{
    public abstract Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game);
    public abstract Task RoundResult(PlayerRoundResult roundResult, Logic.Game game);

    public async Task NewGameState(object? sender, EventArgs e)
    {
        if (sender is Logic.Game { State: GameState.Playing } game)
        {
            await Task.Delay(200); // Note! This is vital in bot play
            await SelectCard(game);
        }
    }

    private async Task SelectCard(Logic.Game game)
    {
        var selectableCards = game.PlayableCards(this);
        var selected = await ChooseCard(selectableCards, game);
        SetSelectedCard(selected);
    }

    public async Task RoundCompleted(object? sender, EventArgs e)
    {
        if (sender is not Logic.Game game) return;
        var roundResult = (RoundResultEventArgs) e;
        var playerAction = roundResult.Actions.FirstOrDefault(x => x.Source.Id == Character.Id);
        var playerResult = new PlayerRoundResult
        {
            GameId = game.Id,
            PlayerId = Id,
            Round = game.Rounds,
            Success = playerAction?.Success ?? false
        };
        await RoundResult(playerResult, game);

        base.NewRound(game, e);
        await SelectCard(game);
    }
}