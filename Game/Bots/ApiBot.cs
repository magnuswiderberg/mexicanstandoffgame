using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Model;
using Common.BotPlay;
using Common.Cards;
using Common.Model;
#pragma warning disable CA1001
#pragma warning disable CA1031

namespace Game.Bots;

public class ApiBot : BotPlayer
{
    private readonly Uri _baseUrl;
    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ApiBot(Uri baseUrl, PlayerId id, string name, Character character) : base(id, name, character)
    {
        _baseUrl = new Uri(baseUrl.AbsoluteUri.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
    {
        try
        {
            var gameActions = CreateGameContext(game.Id, game.Rounds, game.Players, game.Rules, selectableCards);
                
            var url = new Uri(_baseUrl, "actions");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(JsonSerializer.Serialize(gameActions, JsonSerializerOptions), Encoding.UTF8, "application/json");
                
            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Unexpected respsone code: {response.StatusCode}. Content: {result}");
            }

            var card = JsonSerializer.Deserialize<Card>(result, JsonSerializerOptions) ??
                       throw new ArgumentOutOfRangeException($"Got no card from Bot {Id}. Response: '{result}'");

            switch (card.Type)
            {
                case CardType.Dodge:
                case CardType.Load:
                case CardType.Chest:
                    return card;
                case CardType.Attack:
                    var attackCard = JsonSerializer.Deserialize<AttackCard>(result, JsonSerializerOptions);
                    return attackCard!;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectableCards), $"Unknown card type '{card.Type}'");
            }
        }
        catch (Exception e)
        {
            game.LastRound.Errors.Add(new(Id, $"Bot {Name} ({Id}, {_baseUrl}) failed to select card: {e.Message}. (Using Dodge card this round)"));
            return selectableCards.First(x => x.Type == CardType.Dodge);
        }
    }

    public override async Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
    {
        try
        {
            var url = new Uri(_baseUrl, "results");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(JsonSerializer.Serialize(roundResult, JsonSerializerOptions), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Unexpected respsone code: {response.StatusCode}.");
            }
        }
        catch (Exception e)
        {
            game.LastRound.Errors.Add(new(Id, $"Unable to POST RoundResult to bot {Name} ({Id}): {e.Message}."));
        }
    }

    public GameContext CreateGameContext(string gameId, int roundNumber, IReadOnlyList<Player> players, Rules rules, IEnumerable<Card> selectableCards)
    {
        var gameActions = new GameContext
        {
            GameId = gameId,
            RoundNumber = roundNumber,
            SelectableCards = [.. selectableCards],

            Me = new PlayerState
            {
                PlayerId = Id.ToString(),
                Alive = Alive,
                Coins = Coins,
                Shots = Shots,
                Bullets = Bullets
            },
            
            OtherPlayers = [.. players
                .Where(x => x.Id != Id)
                .Select(x => new PlayerState
                {
                    PlayerId = x.Id.Value,
                    Alive = x.Alive,
                    Coins = x.Coins,
                    Shots = x.Shots,
                    Bullets = x.Bullets
                })],

            Rules = new RuleSet
            {
                CoinsToWin = rules.CoinsToWin,
                ShotsToDie = rules.ShotsToDie,
                MaxBullets = rules.MaxBullets,
                ChestsPerPlayerCount = rules.ChestsPerPlayerCount,
            }
        };
        return gameActions;
    }
}