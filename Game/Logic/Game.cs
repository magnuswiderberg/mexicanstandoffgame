using Game.Bots;
using Game.Events;
using Game.Model;
using Shared.Cards;

namespace Game.Logic;

public partial class Game(string id, Rules? rules = null)
{
    // As long as we are deployed to one instance, EventHandlers should work fine

    public event EventHandler? PlayerJoined;
    public event EventHandler? PlayerLeft;
    //public event EventHandler? MonitorRemoved;
    public event EventHandler? GameStateChanged;
    public event EventHandler? RoundResultsCompleted;
    public event EventHandler? RoundCompleted;

    public string Id { get; } = id;
    public Rules Rules { get; } = rules ?? new Rules();

    //public string? MonitorId { get; private set; }
    public int Rounds { get; private set; } = 1;

    public GameState State { get; private set; } = GameState.Created;
    public IReadOnlyList<Player> Players { get; } = new List<Player>();
    public RoundResultEventArgs LastRound { get; private set; } = new();
    public IReadOnlyList<Player> Winners { get; } = new List<Player>();

    public void Start()
    {
        State = GameState.Playing;
        GameStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Restart()
    {
        ((List<Player>)Winners).Clear();
        foreach (var player in Players.ToList())
        {
            player.ResetAll();
            RemovePlayer(player);
        }
        LastRound = new RoundResultEventArgs();
        Rounds = 1;
        State = GameState.Created;
        GameStateChanged?.Invoke(this, EventArgs.Empty);
    }

    //public void SetMonitorId(string monitorId)
    //{
    //    MonitorId = monitorId;
    //}

    //public void RemoveMonitor()
    //{
    //    MonitorId = null;
    //    // TODO: Should we really abort game?
    //    State = GameState.Aborted;
    //    MonitorRemoved?.Invoke(this, EventArgs.Empty);
    //}

    private readonly Dictionary<BotPlayer, AsyncBotEvents> _botEvents = new();

    public void AddPlayer(Player player)
    {
        if (Players.Count >= Rules.MaximumPlayerCount) throw new ArgumentOutOfRangeException(nameof(player), "Can't add another player");
        if (Players.Any(p => p.Id == player.Id)) return;

        // TODO: Should we rest the player here?

        ((List<Player>)Players).Add(player);
        player.CardChanged += PlayerCardChanged;
        RoundResultsCompleted += player.NewRound;
        if (player is BotPlayer bot)
        {
            if (!_botEvents.TryGetValue(bot, out var eventHandler))
            {
                eventHandler = new AsyncBotEvents(bot);
                _botEvents.Add(bot, eventHandler);
            }
            GameStateChanged += eventHandler.GameStateChanged;
            RoundCompleted += eventHandler.RoundCompleted;
        }
        PlayerJoined?.Invoke(this, new PlayerJoinedEvent(player));

        // Check for automatic start
        if (Players.Count >= Rules.MaximumPlayerCount)
        {
            Start();
        }
    }

    public void RemovePlayer(Player player)
    {
        var listPlayer = Players.FirstOrDefault(p => p.Id == player.Id);
        if (listPlayer == null) return;

        if (((List<Player>)Players).Remove(listPlayer))
        {
            RoundResultsCompleted -= listPlayer.NewRound;
            player.CardChanged -= PlayerCardChanged;
            if (listPlayer is BotPlayer bot)
            {
                if (_botEvents.TryGetValue(bot, out var eventHandler))
                {
                    GameStateChanged -= eventHandler.GameStateChanged;
                    RoundCompleted -= eventHandler.RoundCompleted;
                    _botEvents.Remove(bot);
                }
            }
            PlayerLeft?.Invoke(this, new PlayerLeftEvent(listPlayer));
        }
    }

    public void SetRoundCompleted()
    {
        Rounds++;
        RoundCompleted?.Invoke(this, LastRound);
        LastRound = new RoundResultEventArgs();

        const int maxRounds = 10000;
        if (Rounds > maxRounds)
        {
            LastRound.Errors.Add($"The game went on for {maxRounds} rounds. Ending it now.");
            End();
        }
    }

    public List<Character> AllCharacters()
    {
        return Players.Select(x => x.Character).ToList();
    }
}

internal class AsyncBotEvents
{
    public AsyncBotEvents(BotPlayer botPlayer)
    {
        //GameStateChanged = (s, e) => botPlayer.NewGameState(s, e).Wait(TimeSpan.FromSeconds(10));
        //RoundCompleted = (s, e) => botPlayer.RoundCompleted(s, e).Wait(TimeSpan.FromSeconds(10));
        //GameStateChanged = async (s, e) => await botPlayer.NewGameState(s, e);
        //RoundCompleted = async (s, e) => await botPlayer.RoundCompleted(s, e);
        GameStateChanged = (s, e) => botPlayer.NewGameState(s, e);
        RoundCompleted = (s, e) => botPlayer.RoundCompleted(s, e);
    }

    public EventHandler GameStateChanged { get; set; }
    public EventHandler RoundCompleted { get; set; }
}