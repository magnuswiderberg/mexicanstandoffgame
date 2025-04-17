using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Model;
using Shared.BotPlay;
using Shared.Cards;

namespace Game.Bots
{
    public class ApiBot : BotPlayer
    {
        private readonly string _actionUrl;
        private readonly string _roundResultUrl;
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonSerializerOptions;

        static ApiBot()
        {
            JsonSerializerOptions = new JsonSerializerOptions { /*TODO: TypeNameHandling = TypeNameHandling.All*/ };
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public ApiBot(string actionUrl, string roundResultUrl, string authorizationHeader, string id, Character character) : base(id, character)
        {
            _actionUrl = actionUrl;
            _roundResultUrl = roundResultUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        }


        public override async Task<Card> ChooseCard(IReadOnlyList<Card> selectableCards, Logic.Game game)
        {
            try
            {
                var gameActions = CreateGameContext(game.Id, game.Rounds, game.Players, game.Rules, selectableCards);
                var request = new HttpRequestMessage(HttpMethod.Post, _actionUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(gameActions, JsonSerializerOptions), Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected respsone code: {response.StatusCode}. Content: {result}");
                }

                var card = JsonSerializer.Deserialize<Card>(result, JsonSerializerOptions);
                if (card == null) throw new ArgumentOutOfRangeException($"Got no card from Bot {Id}. Response: '{result}'");
                switch (card.Type)
                {
                    case CardType.Dodge:
                    case CardType.Load:
                    case CardType.Chest:
                        return card;
                    case CardType.Attack:
                        var attackCard = JsonSerializer.Deserialize<AttackCard>(result, JsonSerializerOptions);
                        return attackCard;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                game.LastRound.Errors.Add($"ChooseCard: Bot {Id} ({Name}; {_actionUrl}) failed: {e.Message}. (Using Dodge card this round)");
                return selectableCards.FirstOrDefault(x => x.Type == CardType.Dodge);
            }
        }

        public override async Task RoundResult(PlayerRoundResult roundResult, Logic.Game game)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _roundResultUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(roundResult, JsonSerializerOptions), Encoding.UTF8, "application/json")
                };
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected respsone code: {response.StatusCode}.");
                }
            }
            catch (Exception e)
            {
                game.LastRound.Errors.Add($"RoundResult: Bot {Id} ({Name}) failed: {e.Message}. (Using Dodge card this round)");
            }
        }

        public static GameContext CreateGameContext(string gameId, int roundNumber, IEnumerable<Player> players, Rules rules, IEnumerable<Card> selectableCards)
        {
            var gameActions = new GameContext
            {
                GameId = gameId,
                RoundNumber = roundNumber,
                SelectableCards = selectableCards.ToList(),
                
                Players = players.Select(x => new PlayerState
                {
                    Id = x.Id,
                    Character = x.Character,
                    Alive = x.Alive,
                    Coins = x.Coins,
                    Shots = x.Shots,
                    Bullets = x.Bullets
                }).ToList(),

                Rules = new RuleSet
                {
                    CoinsToWin = rules.CoinsToWin,
                    ShotsToDie = rules.ShotsToDie,
                    MaxBullets = rules.MaxBullets
                }
            };
            return gameActions;
        }
    }
}
