using Game.Model;
using Game.Repository;
using Shouldly;
using System.Text.Json.Serialization;
using System.Text.Json;
using Common.Cards;
using Common.GameEvents;
using Common.Model;
using Moq;

#pragma warning disable CA2211
#pragma warning disable CA2007

namespace UnitTests;

public class GameLogicTests
{
    private readonly GameRepository _gameRepository = new();

    private readonly Player _player1 = new(PlayerId.From("1"), Character.Get(1)!);
    private readonly Player _player2 = new(PlayerId.From("2"), Character.Get(2)!);
    private readonly Player _player3 = new(PlayerId.From("3"), Character.Get(3)!);

    private readonly Mock<IGameEvents> _gameEventsMock = new();

    public GameLogicTests()
    {
        _gameEventsMock
            .Setup(m => m.CardPlayedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Card?>()))
            .Callback((string gameId, string _, Card? _) =>
            {
                var game = _gameRepository.GetGame(gameId);
                if (game!.AlivePlayers.All(p => p.SelectedCard != null))
                {
                    game.SetRoundCompletedAsync().Wait();
                }
            });
    }


    #region Helpers

    private async Task<Game.Logic.Game> CreateAndStartGameWithThreePlayers(Rules? rules = null)
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object, rules);
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);
        var addResult = await game.AddPlayerAsync(_player3);
        if (addResult != AddPlayerResultType.Success) throw new InvalidOperationException();
        await game.StartAsync();
        return game;
    }

    private async Task MakeRoundResolve(Game.Logic.Game game)
    {
        foreach (var player in game.Players)
        {
            await player.SetSelectedCardAsync(Card.Dodge);
        }
    }

    #endregion Helpers


    [Fact]
    public async Task A_Game_Can_Be_Created_And_Started_With_Three_Players()
    {
        const int playerCount = 3;
        var game = await CreateAndStartGameWithThreePlayers();
        game.ShouldNotBeNull();
        game.State.ShouldBe(GameState.Playing, "The game should have started");
        game.Rounds.ShouldBe(1, "A game that has just started should be on round 1");
        game.Players.Count.ShouldBe(playerCount);
        game.Winners.ShouldBeEmpty();
        game.AlivePlayers.Count.ShouldBe(playerCount);
    }

    [Fact]
    public async Task An_Existing_Game_Can_Be_Retrieved()
    {
        var createdGame = await CreateAndStartGameWithThreePlayers();
        createdGame.ShouldNotBeNull();

        var game = _gameRepository.GetGame(createdGame.Id);
        game.ShouldNotBeNull();
        game.Id.ShouldBe(createdGame.Id);
        game.Rounds.ShouldBe(createdGame.Rounds);
        game.Players.ShouldBeEquivalentTo(createdGame.Players);
        game.AlivePlayers.ShouldBeEquivalentTo(createdGame.AlivePlayers);
        game.Winners.ShouldBeEquivalentTo(createdGame.Winners);
    }

    [Fact]
    public void When_Retrieving_A_Non_Existing_Game_Null_Is_Returned()
    {
        Should.NotThrow(() =>
        {
            var game = _gameRepository.GetGame("nonexist");
            game.ShouldBeNull();
        });
    }

    [Fact]
    public void Theare_Are_Sufficintly_Many_Game_Ids()
    {
        const int gameIdsToCreate = 10000;
        var gameIds = new HashSet<string>();
        for (var i = 0; i < gameIdsToCreate; i++)
        {
            var gameId = UniqueIdentifier.Create(id => gameIds.Contains(id));
            gameIds.Add(gameId.Id);
        }
        gameIds.Count.ShouldBe(gameIdsToCreate);
    }

    [Fact]
    public async Task A_Player_Cannot_Be_Added_To_A_Game_If_There_Are_No_Seats_Left()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await CreateAndStartGameWithThreePlayers(new Rules { MaximumPlayerCount = 2 });
        });
    }

    [Fact]
    public async Task With_Empty_Gun_Only_Certain_Cards_Are_Playable()
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object);
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);

        var playableCards = game.PlayableCards(_player1);
        playableCards.ShouldBeEquivalentTo(new[] { Card.Dodge, Card.Load, Card.Chest }.ToList());
    }

    [Fact]
    public async Task With_Loaded_Gun_Attack_Cards_Are_Available()
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object);
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);

        _player1.Bullets++;

        var playableCards = game.PlayableCards(_player1);
        playableCards.Any(c => c is AttackCard ac && ac.Target == _player2.Id.Value).ShouldBeTrue();
    }

    [Fact]
    public async Task With_Fully_Loaded_Gun_Attack_Cards_Are_Available_For_Many_Opponents_But_Load_Is_Not()
    {
        const int maxBullets = 6;
        var game = _gameRepository.CreateGame(_gameEventsMock.Object, new Rules { MaxBullets = maxBullets });
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);
        await game.AddPlayerAsync(_player3);
        await game.AddPlayerAsync(new Player(PlayerId.From("4"), Character.Get(4)!));
        await game.AddPlayerAsync(new Player(PlayerId.From("5"), Character.Get(5)!));
        await game.AddPlayerAsync(new Player(PlayerId.From("6"), Character.Get(6)!));
        await game.AddPlayerAsync(new Player(PlayerId.From("7"), Character.Get(7)!));

        _player1.Bullets = maxBullets;

        var playableCards = game.PlayableCards(_player1);
        playableCards.Any(c => c.Type == CardType.Load).ShouldBeFalse();
        foreach (var gamePlayer in game.Players.Except([_player1]))
        {
            playableCards
                .Any(c => c is AttackCard ac && ac.Target == gamePlayer.Id.Value)
                .ShouldBeTrue($"An attack card for {gamePlayer.Character.Id} should be playable");
        }
    }

    //[Fact]
    //public void Number_Of_Playable_Chests()
    //{
    //    var game = _gameRepository.CreateGame(new Rules
    //    {
    //        ChestsPerPlayerCount = new Dictionary<int, int>
    //            {
    //                { 1, 1 },
    //                { 2, 2 },
    //                { 3, 3 }
    //            }
    //    });

    //    game.AddPlayer(_player1);
    //    var playableCards = game.PlayableCards(_player1);
    //    playableCards.ShouldBeEquivalentTo(new[] { Card.Dodge, Card.Load, Card.Chest }.ToList());

    //    game.AddPlayer(_player2);
    //    playableCards = game.PlayableCards(_player1);
    //    playableCards.ShouldBeEquivalentTo(new[] { Card.Dodge, Card.Load, Card.Chest }.ToList());

    //    game.AddPlayer(_player3);
    //    playableCards = game.PlayableCards(_player1);
    //    playableCards.ShouldBeEquivalentTo(new[] { Card.Dodge, Card.Load, Card.Chest }.ToList());
    //}

    [Fact]
    public async Task Player_Gets_Coin_When_Alone_On_Chest()
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object);
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);
        await game.AddPlayerAsync(_player3);
        await game.StartAsync();

        _player1.Coins.ShouldBe(0);

        await _player1.SetSelectedCardAsync(Card.Chest);
        await _player2.SetSelectedCardAsync(Card.Dodge);
        await _player3.SetSelectedCardAsync(Card.Dodge); // This should resolve the round

        _player1.Coins.ShouldBe(1);
    }


#pragma warning disable xUnit1047
    public static IEnumerable<ITheoryDataRow> PlayerGetsCoinWhenSufficentlyFewAreOnTheChestData = [
        new TheoryDataRow<int, int, Dictionary<int, int>>(3, 1, new() { { 3, 1 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(3, 2, new() { { 3, 2 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(3, 3, new() { { 3, 3 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(8, 1, new() { { 8, 1 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(8, 2, new() { { 8, 2 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(8, 3, new() { { 8, 3 } }),
        new TheoryDataRow<int, int, Dictionary<int, int>>(8, 4, new() { { 8, 8 } }),
    ];
#pragma warning restore xUnit1047

    [Theory]
#pragma warning disable xUnit1042
    [MemberData(nameof(PlayerGetsCoinWhenSufficentlyFewAreOnTheChestData))]
#pragma warning restore xUnit1042
    public async Task Player_Gets_Coin_When_Sufficently_Few_Are_On_The_Chest(int players, int playersToGetCoin, Dictionary<int, int> setup)
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object, new Rules { ChestsPerPlayerCount = setup });
        for (var i = 1; i <= players; i++)
        {
            await game.AddPlayerAsync(new Player(PlayerId.From($"{i}"), Character.Get(i)!));
        }
        await game.StartAsync();

        foreach (var gamePlayer in game.Players)
        {
            gamePlayer.Coins.ShouldBe(0);
        }

        for (var i = 1; i <= playersToGetCoin; i++)
        {
            await game.Players.First(p => p.Id.ToString() == $"{i}").SetSelectedCardAsync(Card.Chest);
        }
        for (var i = playersToGetCoin + 1; i <= players; i++)
        {
            await game.Players.First(p => p.Id.ToString() == $"{i}").SetSelectedCardAsync(Card.Dodge);
        }
        game.Rounds.ShouldBe(2, "The first round should have been completed");

        for (var i = 1; i <= playersToGetCoin; i++)
        {
            game.Players.First(p => p.Id.ToString() == $"{i}").Coins.ShouldBe(1, $"Player {i} should get a coin");
        }
    }

    [Fact]
    public async Task Player_Gets_Coin_When_Other_Coin_Player_Gets_Shot()
    {
        await CreateAndStartGameWithThreePlayers();
        _player3.Bullets = 1;

        _player1.Coins.ShouldBe(0);

        await _player1.SetSelectedCardAsync(Card.Chest);
        await _player2.SetSelectedCardAsync(Card.Chest);
        await _player3.SetSelectedCardAsync(new AttackCard(_player2.Id.Value)); // This should resolve the round

        _player1.Coins.ShouldBe(1);
    }

    [Fact]
    public async Task Player_With_Most_Coins_Wins()
    {
        var game = await CreateAndStartGameWithThreePlayers();
        _player1.Coins = game.Rules.CoinsToWin;

        await MakeRoundResolve(game);

        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(1);
        game.Winners[0].Id.ShouldBe(_player1.Id);
    }


    [Fact]
    public async Task One_Of_Players_With_Most_Coins_Wins_By_Fewest_Shots()
    {
        var game = await CreateAndStartGameWithThreePlayers();
        _player1.Coins = game.Rules.CoinsToWin;
        _player1.Bullets = 1;
        _player2.Coins = game.Rules.CoinsToWin;
        _player2.Shots = 1;
        _player2.Bullets = 1;

        await MakeRoundResolve(game);

        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(1);
        game.Winners[0].Id.ShouldBe(_player1.Id);
    }

    [Fact]
    public async Task One_Of_Players_With_Most_Coins_Wins_By_Most_Bullets()
    {
        var game = await CreateAndStartGameWithThreePlayers();
        _player1.Coins = game.Rules.CoinsToWin;
        _player1.Shots = 1;
        _player1.Bullets = 1;
        _player2.Coins = game.Rules.CoinsToWin;
        _player2.Shots = 1;
        _player2.Bullets = 0;

        await MakeRoundResolve(game);

        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(1);
        game.Winners.ShouldBeEquivalentTo(new[] { _player1 }.ToList());
    }

    [Fact]
    public async Task Two_Players_With_Most_Coins_Win_If_Same_Shots_And_Bullets()
    {
        var game = await CreateAndStartGameWithThreePlayers();
        _player1.Coins = game.Rules.CoinsToWin;
        _player1.Shots = 1;
        _player1.Bullets = 1;
        _player2.Coins = game.Rules.CoinsToWin;
        _player2.Shots = 1;
        _player2.Bullets = 1;

        await MakeRoundResolve(game);

        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(2);
        game.Winners.ShouldBeEquivalentTo(new[] { _player1, _player2 }.ToList());
    }

    [Fact]
    public async Task Single_Player_Left_Wins()
    {
        var game = await CreateAndStartGameWithThreePlayers();
        _player2.Shots = game.Rules.MaxBullets;
        _player3.Shots = game.Rules.MaxBullets;

        await MakeRoundResolve(game);

        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(1);
        game.Winners[0].Id.ShouldBe(_player1.Id);
    }

    [Fact]
    public async Task Miss_Attack_When_Opponent_Dodges()
    {
        await CreateAndStartGameWithThreePlayers();
        _player2.Bullets = 1;

        await _player1.SetSelectedCardAsync(Card.Dodge);
        await _player2.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));
        await _player3.SetSelectedCardAsync(Card.Dodge);

        _player1.Shots.ShouldBe(0);
    }

    [Fact]
    public async Task Bullets_Decrease_When_Shooting()
    {
        await CreateAndStartGameWithThreePlayers();
        _player2.Bullets = 1;

        await _player1.SetSelectedCardAsync(Card.Load);
        await _player2.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));
        await _player3.SetSelectedCardAsync(Card.Dodge);

        _player1.Shots.ShouldBe(1);
        _player1.Bullets.ShouldBe(0);
        _player2.Shots.ShouldBe(0);
    }

    [Fact]
    public async Task Shooters_Split_Money_1()
    {
        await CreateAndStartGameWithThreePlayers(new Rules { CoinsToWin = 6, ShotsToDie = 2 });
        _player1.Coins = 3;
        _player2.Bullets = 1;
        _player3.Bullets = 1;

        await _player1.SetSelectedCardAsync(Card.Load);
        await _player2.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));
        await _player3.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));

        _player1.Alive.ShouldBeFalse();
        _player2.Coins.ShouldBe(1);
        _player3.Coins.ShouldBe(1);
    }

    [Fact]
    public async Task Shooters_Split_Money_2()
    {
        await CreateAndStartGameWithThreePlayers(new Rules { CoinsToWin = 6, ShotsToDie = 2 });
        _player1.Coins = 3;
        _player1.Shots = 1;
        _player2.Bullets = 1;

        await _player1.SetSelectedCardAsync(Card.Load);
        await _player2.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));
        await _player3.SetSelectedCardAsync(Card.Load);

        _player1.Alive.ShouldBeFalse();
        _player2.Coins.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    public async Task Shooters_Split_Money_3(int player1Coins, int player2Coins)
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object, new Rules { ShotsToDie = 1 });
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);
        await game.StartAsync();

        _player1.Coins = player1Coins;
        _player1.Bullets = 1;
        _player2.Coins = player2Coins;
        _player2.Bullets = 1;

        await _player1.SetSelectedCardAsync(new AttackCard(_player2.Id.Value));
        await _player2.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));

        _player1.Alive.ShouldBeFalse();
        _player1.Coins.ShouldBe(player1Coins);
        _player2.Coins.ShouldBe(player2Coins);
    }

    [Fact]
    public async Task Shooters_Split_Money_4()
    {
        await CreateAndStartGameWithThreePlayers(new Rules { ShotsToDie = 2 });
        _player1.Bullets = 1;
        _player2.Coins = 2;
        _player2.Bullets = 1;
        _player2.Shots = 1;
        _player3.Bullets = 1;
        _player3.Shots = 1;

        await _player1.SetSelectedCardAsync(new AttackCard(_player2.Id.Value));
        await _player2.SetSelectedCardAsync(new AttackCard(_player3.Id.Value));
        await _player3.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));

        _player1.Alive.ShouldBeTrue();
        _player2.Alive.ShouldBeFalse();
        _player3.Alive.ShouldBeFalse();
        _player1.Coins.ShouldBe(2);
        _player2.Coins.ShouldBe(0);
        _player3.Coins.ShouldBe(0);
    }

    [Fact]
    public async Task Shooters_Split_Money_5()
    {
        await CreateAndStartGameWithThreePlayers(new Rules { ShotsToDie = 2 });
        _player1.Bullets = 1;
        _player2.Coins = 2;
        _player2.Bullets = 1;
        _player3.Coins = 1;
        _player3.Bullets = 1;
        _player3.Shots = 1;

        await _player1.SetSelectedCardAsync(new AttackCard(_player2.Id.Value));
        await _player2.SetSelectedCardAsync(new AttackCard(_player3.Id.Value));
        await _player3.SetSelectedCardAsync(new AttackCard(_player2.Id.Value));

        _player1.Alive.ShouldBeTrue();
        _player2.Alive.ShouldBeFalse();
        _player3.Alive.ShouldBeFalse();
        _player1.Coins.ShouldBe(2);
        _player2.Coins.ShouldBe(0);
        _player3.Coins.ShouldBe(1);
    }

    [Fact]
    public void Todo_Split_Money_In_Weird_Situations()
    {
        // Are there more situations to consider?
    }

    [Fact]
    public async Task When_All_Are_Dead_At_Same_Time_There_Is_No_Winner()
    {
        var game = await CreateAndStartGameWithThreePlayers(new Rules { ShotsToDie = 1 });
        _player1.Coins = 2;
        _player1.Bullets = 1;
        _player2.Coins = 2;
        _player2.Bullets = 1;
        _player3.Coins = 2;
        _player3.Bullets = 1;

        await _player1.SetSelectedCardAsync(new AttackCard(_player2.Id.Value));
        await _player2.SetSelectedCardAsync(new AttackCard(_player3.Id.Value));
        await _player3.SetSelectedCardAsync(new AttackCard(_player1.Id.Value));

        _player1.Alive.ShouldBeFalse();
        _player2.Alive.ShouldBeFalse();
        _player3.Alive.ShouldBeFalse();
        _player1.Coins.ShouldBe(2);
        _player2.Coins.ShouldBe(2);
        _player3.Coins.ShouldBe(2);
        game.State.ShouldBe(GameState.Ended);
        game.Winners.Count.ShouldBe(0);
    }

    [Fact]
    public async Task A_Game_Starts_Automatically_When_Reached_Max_Players()
    {
        var game = _gameRepository.CreateGame(_gameEventsMock.Object, new Rules { MaximumPlayerCount = 3 });
        await game.AddPlayerAsync(_player1);
        await game.AddPlayerAsync(_player2);
        await game.AddPlayerAsync(_player3);

        game.State.ShouldBe(GameState.Playing);
    }

    [Fact]
    public void Todo_Illegal_Card_From_Bot_Player()
    {
        // TODO
    }

    [Fact]
    public void Can_Deserialize_Card()
    {
        var cardAsJsonString = """
                               {
                                "Type": "Load",
                                "Name": "Toad"
                               }
                               """;
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var card = JsonSerializer.Deserialize<Card>(cardAsJsonString, options);
        card.ShouldNotBeNull(cardAsJsonString);
        card.Type.ShouldBe(CardType.Load);
        card.Name.ShouldBe("Toad");
    }
}
