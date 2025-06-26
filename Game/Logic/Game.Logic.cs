using Game.Model;
using Common.Cards;
using Common.Model;

namespace Game.Logic;

public partial class Game
{
    public IReadOnlyList<Card> PlayableCards(Player player)
    {
        var playable = new List<Card> { Card.Dodge, Card.Chest };

        if (player.Bullets < Rules.MaxBullets) playable.Add(Card.Load);

        if (player.Bullets != 0)
        {
            playable.AddRange(AlivePlayers
                .Where(x => x.Id != player.Id)
                .Select(otherPlayer => new AttackCard(otherPlayer.Id.Value)));
        }

        playable.Sort((a, b) => a.Type - b.Type);
        return playable;
    }

    private async Task PlayerCardChangedAsync(Player player)
    {
        if (State != GameState.Playing) return;

        await gameEvents.CardPlayedAsync(Id, player.Id.Value, player.SelectedCard);

        bool roundCompleted = false;
#pragma warning disable CA2002
        lock (Id)
        {
            try
            {
                Players.ToList().ForEach(x => x.SetLocked(true));
                if (AlivePlayers.All(x => x.SelectedCard != null))
                {
                    // All players have selected their card; resolve
                    CompleteRound();
                    roundCompleted = true;
                }
            }
            finally
            {
                Players.ToList().ForEach(x => x.SetLocked(false));
            }
        }
#pragma warning restore CA2002

        if (roundCompleted)
        {
            await MaybeEndGameAsync();
            await gameEvents.RoundResultsCompletedAsync(Id);
            if (State == GameState.Playing)
            {
                foreach (var p in Players)
                {
                    p.NewRound();
                }
            }
        }
    }

    public IReadOnlyList<Player> AlivePlayers => [.. Players.Where(x => x.Shots < Rules.ShotsToDie)];

    private void CompleteRound()
    {
        var diffInitializer = AlivePlayers.ToDictionary(p => p.Id, _ => 0);
        var coinsDiff = new Dictionary<PlayerId, int>(diffInitializer);
        var shotsDiff = new Dictionary<PlayerId, int>();
        var shotBy = new Dictionary<PlayerId, List<Player>>(Players.ToDictionary(p => p.Id, _ => new List<Player>()));
        var bulletsDiff = new Dictionary<PlayerId, int>(diffInitializer);
        var result = new Dictionary<PlayerId, bool>();
        var actions = new List<RoundAction>();

        // Dodge
        foreach (var player in AlivePlayers.Where(player => player.SelectedCard?.Type == CardType.Dodge).ToList())
        {
            result[player.Id] = true;
            actions.Add(new RoundAction(RoundActionType.Dodge, player.Id, result[player.Id]));
        }

        // Attacks
        foreach (var player in AlivePlayers
                     .Where(player => player.SelectedCard is AttackCard)
                     .Where(player => player.Bullets > 0)
                     .ToList())
        {
            var attackCard = player.SelectedCard as AttackCard;
            var attackedPlayer = Players.FirstOrDefault(x => x.Id.Value == attackCard?.Target);
            if (attackedPlayer == null)
            {
                // TODO: Should not happen
            }
            else
            {
                // Can't attack dodging player
                if (attackedPlayer.SelectedCard?.Type != CardType.Dodge)
                {
                    if (!shotsDiff.TryAdd(attackedPlayer.Id, 1)) shotsDiff[attackedPlayer.Id]++;
                    result[player.Id] = true;
                    shotBy[attackedPlayer.Id].Add(player);
                }
                else
                {
                    result[player.Id] = false;
                }
                actions.Add(new RoundAction(RoundActionType.Attack, player.Id, result[player.Id]) { Target = attackedPlayer.Id });
            }
            bulletsDiff[player.Id] = -1;
        }

        // Loads
        foreach (var player in AlivePlayers.Where(player => player.SelectedCard?.Type == CardType.Load).ToList())
        {
            if (shotsDiff.ContainsKey(player.Id))
            {
                result[player.Id] = false;
            }
            else
            {
                if (player.Bullets < Rules.MaxBullets)
                {
                    bulletsDiff[player.Id]++;
                    result[player.Id] = true;
                }
            }
            actions.Add(new RoundAction(RoundActionType.Load, player.Id, result[player.Id]));
        }

        // Chests
        foreach (var player in AlivePlayers.Where(x => x.SelectedCard?.Type == CardType.Chest).ToList())
        {
            if (shotsDiff.ContainsKey(player.Id))
            {
                result[player.Id] = false;
            }
            else
            {
                var othersOnChest = AlivePlayers
                    .Where(p => p.Id != player.Id)
                    .Where(p => p.SelectedCard?.Type == CardType.Chest)
                    .Where(p => !shotsDiff.ContainsKey(p.Id))
                    .ToList();
                var maxOnChest = Rules.ChestsPerPlayerCount.ContainsKey(AlivePlayers.Count)
                    ? Rules.ChestsPerPlayerCount[AlivePlayers.Count]
                    : 1;
                result[player.Id] = othersOnChest.Count <= maxOnChest - 1; // TODO: unit test
                if (result[player.Id])
                {
                    coinsDiff[player.Id]++;
                }
            }
            actions.Add(new RoundAction(RoundActionType.Chest, player.Id, result[player.Id]));
        }

        foreach (var action in actions)
        {
            if (shotsDiff.Keys
                .Select(playerId => Players.First(p => p.Id == playerId))
                .Any(player => player.Id == action.Source))
            {
                action.Shot = true;
            }
        }

        // Apply
        foreach (var entry in coinsDiff)
        {
            var player = Players.First(x => x.Id == entry.Key);
            player.Coins += entry.Value;
        }

        foreach (var entry in shotsDiff)
        {
            var player = Players.First(x => x.Id == entry.Key);
            player.Shots += entry.Value;
            if (player.Shots >= Rules.ShotsToDie)
            {
                player.SetDead();
            }
        }
        foreach (var (playerId, bullets) in bulletsDiff)
        {
            var player = Players.First(x => x.Id == playerId);
            player.Bullets += bullets;
        }
        foreach (var player in Players.ToList())
        {
            if (player.Alive == false)
            {
                var aliveShooters = shotBy[player.Id].Where(x => x.Alive).ToList();
                if (aliveShooters.Count != 0)
                {
                    var dividedCoins = player.Coins / aliveShooters.Count;
                    foreach (var shooter in aliveShooters)
                    {
                        shooter.Coins += dividedCoins;
                    }
                    player.Coins = 0;
                }
            }
        }

        foreach (var entry in result)
        {
            var player = Players.First(x => x.Id == entry.Key);
            player.SetResult(Rounds, entry.Value);
        }

        actions = [.. actions
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Source.Value)
        ];
        LastRound.Actions = actions;
    }

    private async Task MaybeEndGameAsync()
    {
        var winners = new List<Player>();

        // Enough coins
        var playersWithEnoughCoins = AlivePlayers.Where(x => x.Coins >= Rules.CoinsToWin).ToList();
        if (playersWithEnoughCoins.Count != 0)
        {
            // Most coins wins
            var playersWithMostCoins = playersWithEnoughCoins
                .GroupBy(x => x.Coins)
                .OrderByDescending(g => g.Key)
                .Where(g => g.Key == playersWithEnoughCoins.Select(x => x.Coins).Max())
                .SelectMany(g => g)
                .ToList();

            // Fewest shots is tiebreaker
            winners.AddRange(playersWithMostCoins
                .GroupBy(x => x.Shots)
                .OrderBy(g => g.Key)
                .Where(g => g.Key == playersWithMostCoins.Select(x => x.Shots).Min())
                .SelectMany(g => g));

            // Most bullets is tiebreaker
            if (winners.Count > 1)
            {
                var winnersCopy = winners.ToList();
                winners = [.. winners
                    .GroupBy(x => x.Bullets)
                    .OrderBy(g => g.Key)
                    .Where(g => g.Key == winnersCopy.Select(x => x.Bullets).Max())
                    .SelectMany(g => g)
                    ];
            }
        }

        // Alone wins
        else if (AlivePlayers.Count == 1)
        {
            winners.Add(AlivePlayers[0]);
        }

        if (winners.Count != 0)
        {
            ((List<Player>)Winners).AddRange(winners);
            foreach (var player in Winners.ToList()) player.SetWinner();
            await EndAsync();
        }

        // All dead?
        if (!AlivePlayers.Any())
        {
            await EndAsync();
        }
    }

    private async Task EndAsync()
    {
        State = GameState.Ended;
        await gameEvents.GameStateChangedAsync(Id, State);
    }

    public async Task AbortAsync()
    {
        State = GameState.Aborted;
        await gameEvents.GameStateChangedAsync(Id, State);
    }

    public List<AggregatedRoundAction> CreateLastRoundAggregate()
    {
        var aggregatedRoundResult = new List<AggregatedRoundAction>();

        var unsuccessfulAttackers = LastRound.Actions.Where(a => a is { Type: RoundActionType.Attack, Success: false });
        foreach (var action in unsuccessfulAttackers)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == action.Target);
            if (targetPlayer == null) continue;
            var aggregatedAction = aggregatedRoundResult.FirstOrDefault(a => a.TargetPlayers.Any(t => t.Id == targetPlayer.Id));
            if (aggregatedAction == null)
            {
                aggregatedAction = new AggregatedRoundAction
                {
                    TargetPlayers = [targetPlayer],
                    Type = RoundActionType.Dodge,
                    Successful = false,
                    Attackers = []
                };
                aggregatedRoundResult.Add(aggregatedAction);
            }
            var sourcePlayer = Players.FirstOrDefault(p => p.Id == action.Source);
            if (sourcePlayer == null) continue;
            aggregatedAction.Attackers!.Add(sourcePlayer);
        }

        var successfulAttackers = LastRound.Actions.Where(a => a is { Type: RoundActionType.Attack, Success: true });
        foreach (var action in successfulAttackers)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == action.Target);
            if (targetPlayer == null) continue;
            var sourcePlayer = Players.FirstOrDefault(p => p.Id == action.Source);
            if (sourcePlayer == null) continue;
            var aggregatedAction = aggregatedRoundResult.FirstOrDefault(a => a.TargetPlayers.Any(t => t.Id == targetPlayer.Id));
            if (aggregatedAction == null)
            {
                aggregatedAction = new AggregatedRoundAction
                {
                    TargetPlayers = [targetPlayer],
                    Type = RoundActionType.Attack,
                    Successful = true,
                    Attackers = []
                };
                aggregatedRoundResult.Add(aggregatedAction);
            }
            aggregatedAction.Attackers!.Add(sourcePlayer);
        }

        var dodgersWithoutShooter = LastRound.Actions.Where(a => a is { Type: RoundActionType.Dodge });
        var attackerActions = aggregatedRoundResult.ToList();
        var dodgers = new List<Player>();
        foreach (var dodger in dodgersWithoutShooter)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == dodger.Source);
            if (targetPlayer != null)
            {
                var hasAttacker = attackerActions.Exists(a => a.TargetPlayers.Any(t => t.Id == targetPlayer.Id));
                if (hasAttacker) continue;
                dodgers.Add(targetPlayer);
            }
        }
        if (dodgers.Count != 0)
        {
            aggregatedRoundResult.Add(new AggregatedRoundAction
            {
                TargetPlayers = dodgers,
                Type = RoundActionType.Dodge,
                Successful = true
            });
        }

        var successfulLoaders = LastRound.Actions.Where(a => a is { Type: RoundActionType.Load, Success: true });
        var loaders = new List<Player>();
        foreach (var loader in successfulLoaders)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == loader.Source);
            if (targetPlayer == null) continue;
            loaders.Add(targetPlayer);
        }
        if (loaders.Count != 0)
        {
            aggregatedRoundResult.Add(new AggregatedRoundAction
            {
                TargetPlayers = loaders,
                Type = RoundActionType.Load,
                Successful = true
            });
        }

        var successfulChesters = LastRound.Actions.Where(a => a is { Type: RoundActionType.Chest, Success: true });
        var chesters = new List<Player>();
        foreach (var loader in successfulChesters)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == loader.Source);
            if (targetPlayer == null) continue;
            chesters.Add(targetPlayer);
        }
        if (chesters.Count != 0)
        {
            aggregatedRoundResult.Add(new AggregatedRoundAction
            {
                TargetPlayers = chesters,
                Type = RoundActionType.Chest,
                Successful = true
            });
        }

        var unsuccessfulChesters = LastRound.Actions.Where(a => a is { Type: RoundActionType.Chest, Success: false, Shot: false });
        var unchesters = new List<Player>();
        foreach (var loader in unsuccessfulChesters)
        {
            var targetPlayer = Players.FirstOrDefault(p => p.Id == loader.Source);
            if (targetPlayer == null) continue;
            unchesters.Add(targetPlayer);
        }
        if (unchesters.Count != 0)
        {
            aggregatedRoundResult.Add(new AggregatedRoundAction
            {
                TargetPlayers = unchesters,
                Type = RoundActionType.Chest,
                Successful = false
            });
        }

        if (LastRound.Errors.Count != 0)
        {
            foreach (var roundResultError in LastRound.Errors)
            {
                aggregatedRoundResult.Add(new AggregatedRoundAction
                {
                    TargetPlayers = [Players.First(p => p.Id == roundResultError.PlayerId)],
                    Type = RoundActionType.Error,
                    Successful = false,
                    Error = roundResultError.Error
                });
            }
        }

        if (aggregatedRoundResult.Count == 0)
        {
            aggregatedRoundResult.Add(new AggregatedRoundAction
            {
                TargetPlayers = [new Player(PlayerId.From(Guid.NewGuid().ToString()), Character.Get(1)!) { Name = "Unknown error..." }],
                Type = RoundActionType.Dodge,
                Successful = false
            });
        }

        return aggregatedRoundResult;
    }
}