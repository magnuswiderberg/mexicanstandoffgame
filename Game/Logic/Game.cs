using Game.Model;
using Common.Cards;
using Common.GameEvents;
using Common.Model;

namespace Game.Logic;

public partial class Game(string id, IGameEvents gameEvents, Rules? rules = null)
{
    public string Id { get; } = id;
    public Rules Rules { get; } = rules ?? new Rules();

    public int Rounds { get; private set; }

    public GameState State { get; private set; } = GameState.Created;
    public IReadOnlyList<Player> Players { get; } = (List<Player>) [];
    public RoundResult LastRound { get; private set; } = new();
    public IReadOnlyList<Player> Winners { get; } = (List<Player>) [];

    public async Task StartAsync()
    {
        State = GameState.Playing;
        await gameEvents.GameStateChangedAsync(Id, State);
        await gameEvents.NewRoundAsync(Id);
    }

    public async Task RestartAsync()
    {
        await gameEvents.GameRestarted(Id);
        ((List<Player>)Winners).Clear();
        foreach (var player in Players.ToList())
        {
            player.ResetAll();
            await RemovePlayerAsync(player);
        }
        LastRound = new RoundResult();
        Rounds = 0;
        State = GameState.Created;
        await gameEvents.GameStateChangedAsync(Id, State);
    }

    public async Task<AddPlayerResultType> AddPlayerAsync(Player player)
    {
        if (Players.Count >= Rules.MaximumPlayerCount) return AddPlayerResultType.NoSeatsLeft;
        if (Players.Any(p => string.Equals(p.Name, player.Name, StringComparison.OrdinalIgnoreCase))) return AddPlayerResultType.NameTaken;
        if (Players.Any(p => p.Id == player.Id)) return AddPlayerResultType.AlreadyAdded;

        ((List<Player>)Players).Add(player);
        player.CardChanged = async (p) =>
        {
            await PlayerCardChangedAsync(p);
        };
        await gameEvents.PlayerJoinedAsync(Id, player.Id.Value);

        // Check for automatic start
        if (Players.Count >= Rules.MaximumPlayerCount)
        {
            await StartAsync();
        }

        return AddPlayerResultType.Success;
    }

    public async Task RemovePlayerAsync(Player player)
    {
        var listPlayer = Players.FirstOrDefault(p => p.Id == player.Id);
        if (listPlayer != null)
        {
            if (((List<Player>)Players).Remove(listPlayer))
            {
                await gameEvents.PlayerLeftAsync(Id, player.Id.Value);
            }
        }
        var winnerPlayer = Winners.FirstOrDefault(p => p.Id == player.Id);
        if (winnerPlayer != null)
        {
            ((List<Player>)Winners).Remove(winnerPlayer);
        }

        if (State == GameState.Playing)
        {
            await player.SetSelectedCardAsync(null);
            await PlayerCardChangedAsync(player);
        }
    }

    public async Task StartNewRoundAsync()
    {
        Rounds++;
        await gameEvents.NewRoundAsync(Id);
    }

    public async Task SetRoundCompletedAsync()
    {
        await gameEvents.RoundCompletedAsync(Id, LastRound);

        LastRound = new RoundResult();

        const int maxRounds = 10000;
        if (Rounds > maxRounds)
        {
            LastRound.Errors.Add(new(PlayerId.From("_"), $"The game went on for {maxRounds} rounds. Ending it now."));
            await EndAsync();
        }
    }

    public List<Character> AllCharacters()
    {
        return [.. Players.Select(x => x.Character)];
    }
}