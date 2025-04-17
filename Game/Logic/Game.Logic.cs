using Game.Model;
using Shared.Cards;

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
                .Select(otherPlayer => new AttackCard(otherPlayer.Character)));
        }

        playable.Sort((a, b) => a.Type - b.Type);
        return playable;
    }

    private void PlayerCardChanged(object? sender, EventArgs e)
    {
        if (State != GameState.Playing) return;

        if (sender is Player)
        {
#pragma warning disable CA2002
            lock (this)
            {
                try
                {
                    Players.ToList().ForEach(x => x.SetLocked(true));
                    if (AlivePlayers.All(x => x.SelectedCard != null))
                    {
                        // All players have selected their card; resolve
                        CompleteRound();
                        MaybeEndGame();
                    }
                }
                finally
                {
                    Players.ToList().ForEach(x => x.SetLocked(false));
                }
            }
#pragma warning restore CA2002
        }
    }

    public IReadOnlyList<Player> AlivePlayers => Players.Where(x => x.Shots < Rules.ShotsToDie).ToList();

    private void CompleteRound()
    {
        var diffInitializer = AlivePlayers.ToDictionary(p => p, _ => 0);
        var coinsDiff = new Dictionary<Player, int>(diffInitializer);
        var shotsDiff = new Dictionary<Player, int>();
        var shotBy = new Dictionary<Player, List<Player>>(AlivePlayers.ToDictionary(p => p, _ => new List<Player>()));
        var bulletsDiff = new Dictionary<Player, int>(diffInitializer);
        var result = new Dictionary<Player, bool>();
        var actions = new List<RoundAction>();

        // Dodge
        foreach (var player in AlivePlayers.Where(player => player.SelectedCard?.Type == CardType.Dodge).ToList())
        {
            result[player] = true;
            actions.Add(new RoundAction(RoundActionType.Dodge, player.Character, result[player]));
        }

        // Attacks
        foreach (var player in AlivePlayers
                     .Where(player => player.SelectedCard is AttackCard)
                     .Where(player => player.Bullets != 0)
                     .ToList())
        {
            var attackCard = player.SelectedCard as AttackCard;
            var attackedPlayer = Players.First(x => x.Character.Id.Equals(attackCard?.Target.Id));

            // Can't attack dodging player
            if (attackedPlayer.SelectedCard?.Type != CardType.Dodge)
            {
                if (!shotsDiff.TryAdd(attackedPlayer, 1)) shotsDiff[attackedPlayer]++;
                result[player] = true;
                shotBy[attackedPlayer].Add(player);
            }
            else
            {
                result[player] = false;
            }
            bulletsDiff[player] = -1;
            actions.Add(new RoundAction(RoundActionType.Attack, player.Character, result[player]) { Target = attackedPlayer.Character });
        }

        // Loads
        foreach (var player in AlivePlayers.Where(player => player.SelectedCard?.Type == CardType.Load).ToList())
        {
            if (shotsDiff.ContainsKey(player))
            {
                result[player] = false;
            }
            else
            {
                if (player.Bullets < Rules.MaxBullets)
                {
                    bulletsDiff[player]++;
                    result[player] = true;
                }
            }
            actions.Add(new RoundAction(RoundActionType.Load, player.Character, result[player]));
        }

        // Chests
        foreach (var player in AlivePlayers.Where(x => x.SelectedCard?.Type == CardType.Chest).ToList())
        {
            if (shotsDiff.ContainsKey(player))
            {
                result[player] = false;
            }
            else
            {
                var othersOnChest = AlivePlayers
                    .Where(p => p.Id != player.Id)
                    .Where(p => p.SelectedCard?.Type == CardType.Chest)
                    .Where(p => !shotsDiff.ContainsKey(p))
                    .ToList();
                var maxOnChest = Rules.ChestsPerPlayerCount.ContainsKey(AlivePlayers.Count)
                    ? Rules.ChestsPerPlayerCount[AlivePlayers.Count]
                    : 1;
                result[player] = othersOnChest.Count <= maxOnChest - 1; // TODO: unit test
                if (result[player])
                {
                    coinsDiff[player]++;
                }
            }
            actions.Add(new RoundAction(RoundActionType.Chest, player.Character, result[player]));
        }

        foreach (var action in actions)
        {
            if (shotsDiff.Keys.Select(x => x.Character).Any(character => character.Id == action.Source.Id))
            {
                action.Shot = true;
            }
        }

        // Apply
        foreach (var entry in coinsDiff) entry.Key.Coins += entry.Value;
        foreach (var entry in shotsDiff) entry.Key.Shots += entry.Value;
        foreach (var (player, bullets) in bulletsDiff)
        {
            player.Bullets += bullets;
            if (player.Shots >= Rules.ShotsToDie) player.SetDead();
        }
        foreach (var entry in bulletsDiff)
        {
            var player = entry.Key;
            if (!player.Alive)
            {
                var aliveShooters = shotBy[player].Where(x => x.Alive).ToList();
                if (aliveShooters.Count != 0)
                {
                    foreach (var shooter in aliveShooters)
                    {
                        shooter.Coins += player.Coins / aliveShooters.Count;
                    }
                    player.Coins = 0;
                }
            }
        }
        foreach (var entry in result) entry.Key.SetResult(Rounds, entry.Value);

        actions.Sort((a, b) => a.Type - b.Type);
        LastRound.Actions = actions;

        // Event
        RoundResultsCompleted?.Invoke(this, EventArgs.Empty);
        //Rounds++;
    }

    private void MaybeEndGame()
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
                winners = winners
                    .GroupBy(x => x.Bullets)
                    .OrderBy(g => g.Key)
                    .Where(g => g.Key == winners.Select(x => x.Bullets).Max())
                    .SelectMany(g => g)
                    .ToList();
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
            foreach (var player in Winners) player.SetWinner();
            End();
        }

        // All dead?
        if (!AlivePlayers.Any())
        {
            End();
        }
    }

    private void End()
    {
        State = GameState.Ended;
        GameStateChanged?.Invoke(this, EventArgs.Empty);
    }
}