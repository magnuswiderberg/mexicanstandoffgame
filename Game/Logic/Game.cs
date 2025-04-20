using Game.Model;
using Shared.Cards;
using Shared.GameEvents;
using Shared.Model;

namespace Game.Logic;

public partial class Game(string id, IGameEvents gameEvents, Rules? rules = null)
{
    public string Id { get; } = id;
    public Rules Rules { get; } = rules ?? new Rules();

    public int Rounds { get; private set; } = 1;

    public GameState State { get; private set; } = GameState.Created;
    public IReadOnlyList<Player> Players { get; } = new List<Player>();
    public RoundResult LastRound { get; private set; } = new();
    public IReadOnlyList<Player> Winners { get; } = new List<Player>();

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
        Rounds = 1;
        State = GameState.Created;
        await gameEvents.GameStateChangedAsync(Id, State);
    }

    public async Task AddPlayerAsync(Player player)
    {
        if (Players.Count >= Rules.MaximumPlayerCount) throw new ArgumentOutOfRangeException(nameof(player), "Can't add another player");
        if (Players.Any(p => p.Id == player.Id)) return;

        ((List<Player>)Players).Add(player);
        player.CardChanged = async (p) => { await PlayerCardChangedAsync(p); };
        await gameEvents.PlayerJoinedAsync(Id, player.Id);

        // Check for automatic start
        if (Players.Count >= Rules.MaximumPlayerCount)
        {
            await StartAsync();
        }
    }

    public async Task RemovePlayerAsync(Player player)
    {
        var listPlayer = Players.FirstOrDefault(p => p.Id == player.Id);
        if (listPlayer == null) return;

        if (((List<Player>)Players).Remove(listPlayer))
        {
            await gameEvents.PlayerLeftAsync(Id, player.Id);
        }
    }

    public async Task SetRoundCompletedAsync()
    {
        Rounds++;
        await gameEvents.RoundCompletedAsync(Id, LastRound);

        LastRound = new RoundResult();

        const int maxRounds = 10000;
        if (Rounds > maxRounds)
        {
            LastRound.Errors.Add($"The game went on for {maxRounds} rounds. Ending it now.");
            await EndAsync();
        }
    }

    public List<Character> AllCharacters()
    {
        return Players.Select(x => x.Character).ToList();
    }
}