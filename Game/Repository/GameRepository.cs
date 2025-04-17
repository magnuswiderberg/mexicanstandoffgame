using Game.Model;
using Microsoft.Extensions.Caching.Memory;

namespace Game.Repository;

public class GameRepository : IDisposable
{
    private readonly MemoryCache _currentGames = new(new MemoryCacheOptions());

    public const string DeveloperGameId = "dev";

    public Logic.Game CreateGame(Rules? rules = null)
    {
        var gameId = UniqueIdentifier.Create(id => _currentGames.TryGetValue(id, out _));
        var newGame = _currentGames.GetOrCreate(gameId.Id, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(6);

            var game = new Logic.Game(gameId.Id, rules);
            return game;
        });

        return newGame!;
    }

    public Logic.Game GetOrCreateDevelopmentGame(ForceRecreate? forceRecreate = null)
    {
        var game = GetGame(DeveloperGameId);

        if (game != null && forceRecreate?.Value == true)
        {
            _currentGames.Remove(DeveloperGameId);
            game = null;
        }

        if (game == null)
        {
            game = new Logic.Game(DeveloperGameId, new Rules
            {
                MinimumPlayerCount = 1,
                MaximumPlayerCount = 4,
                ShotsToDie = 1,
                SelectCardTimeoutSeconds = 0,
                ChestsPerPlayerCount = new Dictionary<int, int>
                {
                    { 1, 1 },
                    { 2, 2 },
                    { 3, 2 },
                    { 4, 3 },
                    { 5, 3 },
                }
            });
            _currentGames.Set(game.Id, game, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(6)
            });
        }
        else
        {
            foreach (var player in game.Players.ToList())
            {
                game.RemovePlayer(player);
                player.ResetCards();
            }
        }

        return game;
    }

    public Logic.Game? GetGame(string? id)
    {
        if (id == null) return null;

        return !_currentGames.TryGetValue(id, out Logic.Game? game) ? null : game;
    }

    public void Dispose()
    {
        _currentGames.Dispose();
    }
}

public record ForceRecreate(bool Value);
