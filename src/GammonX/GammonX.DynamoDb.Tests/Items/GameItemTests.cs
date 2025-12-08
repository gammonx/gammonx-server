using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class GameItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public GameItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateAndSearchMultipleGames()
        {
            var matchId = Guid.NewGuid();
            var portesId = Guid.NewGuid();
            var plakotoId = Guid.NewGuid();
            var fevgaId = Guid.NewGuid();

            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            var wonMatch = ItemFactory.CreateMatch(matchId, player, MatchResult.Won, MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, opponent, MatchResult.Won, MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);

            var portesWin = ItemFactory.CreateGame(portesId, wonMatch, player, GameResult.Single, GameModus.Portes);
            await _repo.SaveAsync(portesWin);
            var plakotoWin = ItemFactory.CreateGame(plakotoId, wonMatch, player, GameResult.Single, GameModus.Plakoto);
            await _repo.SaveAsync(plakotoWin);
            var fevgaWin = ItemFactory.CreateGame(fevgaId, wonMatch, player, GameResult.Single, GameModus.Fevga);
            await _repo.SaveAsync(fevgaWin);

            var portesLoss = ItemFactory.CreateGame(portesId, lostMatch, opponent, GameResult.LostSingle, GameModus.Portes);
            await _repo.SaveAsync(portesLoss);
            var plakotoLoss = ItemFactory.CreateGame(plakotoId, lostMatch, opponent, GameResult.LostSingle, GameModus.Plakoto);
            await _repo.SaveAsync(plakotoLoss);
            var fevgaLoss = ItemFactory.CreateGame(fevgaId, lostMatch, opponent, GameResult.LostSingle, GameModus.Fevga);
            await _repo.SaveAsync(fevgaLoss);

            // get all games from a match
            var allGamesFromMatch = await _repo.GetItemsAsync<GameItem>(matchId, "GAME#");
            Assert.True(allGamesFromMatch.All(agfm => agfm.MatchId.Equals(matchId)));
            Assert.Equal(6, allGamesFromMatch.Count());
            // get all portes games from a match
            var allPortesGames = await _repo.GetItemsAsync<GameItem>(matchId, $"GAME#{portesId}");
            Assert.Equal(2, allPortesGames.Count());
            // get all plakoto games from a match
            var allPlakotoGames = await _repo.GetItemsAsync<GameItem>(matchId, $"GAME#{plakotoId}");
            Assert.Equal(2, allPlakotoGames.Count());
            // get all fevga games from a match
            var allFevgaGames = await _repo.GetItemsAsync<GameItem>(matchId, $"GAME#{fevgaId}");
            Assert.Equal(2, allFevgaGames.Count());

            // get all portes wins from a match
            var portesWins = await _repo.GetItemsAsync<GameItem>(matchId, $"GAME#{portesId}#WON");
            Assert.Single(portesWins);
            // get all portes losses from a match
            var portesLosses = await _repo.GetItemsAsync<GameItem>(matchId, $"GAME#{portesId}#LOST");
            Assert.Single(portesLosses);
            // get all games from a match by player
            var playersGames = await _repo.GetItemsByGSIPKAsync<GameItem>(player.Id);
            Assert.Equal(3, playersGames.Count());
            // get all games from a match by opponent
            var opponentGames = await _repo.GetItemsByGSIPKAsync<GameItem>(opponent.Id);
            Assert.Equal(3, opponentGames.Count());
            // get all portes games from a match by player
            var playersPortesGames = await _repo.GetItemsByGSIPKAsync<GameItem>(player.Id, "GAME#Portes");
            Assert.Single(playersPortesGames);
            // get all portes wins from a match by player
            var playersPortesWins = await _repo.GetItemsByGSIPKAsync<GameItem>(player.Id, "GAME#Portes#WON");
            Assert.Single(playersPortesWins);
            // get all portes losses from a match by player
            var playersPortesLosses = await _repo.GetItemsByGSIPKAsync<GameItem>(player.Id, "GAME#Portes#LOST");
            Assert.Empty(playersPortesLosses);
            // get all portes games from a match by opponent
            var opponentsPortesGames = await _repo.GetItemsByGSIPKAsync<GameItem>(opponent.Id, "GAME#Portes");
            Assert.Single(opponentsPortesGames);
            // get all portes wins from a match by opponent
            var opponentsPortesWins = await _repo.GetItemsByGSIPKAsync<GameItem>(opponent.Id, "GAME#Portes#WON");
            Assert.Empty(opponentsPortesWins);
            // get all portes losses from a match by opponent
            var opponentsPortesLosses = await _repo.GetItemsByGSIPKAsync<GameItem>(opponent.Id, "GAME#Portes#LOST");
            Assert.Single(opponentsPortesLosses);

            await _repo.DeleteAsync<GameItem>(portesWin.MatchId, portesWin.SK);
            await _repo.DeleteAsync<GameItem>(portesLoss.MatchId, portesLoss.SK);
            await _repo.DeleteAsync<GameItem>(plakotoWin.MatchId, plakotoWin.SK);
            await _repo.DeleteAsync<GameItem>(plakotoLoss.MatchId, plakotoLoss.SK);
            await _repo.DeleteAsync<GameItem>(fevgaWin.MatchId, fevgaWin.SK);
            await _repo.DeleteAsync<GameItem>(fevgaLoss.MatchId, fevgaLoss.SK);

            allGamesFromMatch = await _repo.GetItemsAsync<GameItem>(matchId, "GAME#");
            Assert.Empty(allGamesFromMatch);
        }

        [Fact]
        public async Task CreateSearchUpdateDeleteGame()
        {
            var player = ItemFactory.CreatePlayer();
            var match = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);
            var game = ItemFactory.CreateGame(Guid.NewGuid(), match, player, GameResult.Single, GameModus.Portes);
            // create
            await _repo.SaveAsync(game);
            // read
            var games = await _repo.GetItemsAsync<GameItem>(match.Id, "GAME#");
            Assert.NotNull(games);
            Assert.Single(games);
            var gameFromRepo = games.First();
            Assert.Equal($"MATCH#{match.Id}", gameFromRepo.PK);
            Assert.Equal($"GAME#{game.Id}#WON", gameFromRepo.SK);
            Assert.Equal($"PLAYER#{player.Id}", gameFromRepo.GSI1PK);
            Assert.Equal($"GAME#Portes#WON", gameFromRepo.GSI1SK);
            Assert.Equal(ItemTypes.GameItemType, gameFromRepo.ItemType);
            Assert.Equal(game.Id, gameFromRepo.Id);
            Assert.Equal(player.Id, gameFromRepo.PlayerId);
            Assert.Equal(match.Id, gameFromRepo.MatchId);
            Assert.Equal(GameResult.Single, gameFromRepo.Result);
            Assert.Equal(GameModus.Portes, gameFromRepo.Modus);
            Assert.Equal(1, gameFromRepo.Points);
            Assert.Equal(66, gameFromRepo.Length);
            Assert.Equal(10, gameFromRepo.DiceDoubles);
            Assert.Equal(0, gameFromRepo.PipesLeft);
            Assert.Equal(TimeSpan.FromMinutes(10), gameFromRepo.Duration);
            Assert.Null(gameFromRepo.DoublingCubeValue);
            // update
            gameFromRepo.Points++;
            await _repo.SaveAsync(gameFromRepo);
            games = await _repo.GetItemsAsync<GameItem>(match.Id, "GAME#");
            Assert.NotNull(games);
            Assert.Single(games);
            gameFromRepo = games.First();
            Assert.Equal(2, gameFromRepo.Points);
            // delete
            var deleted = await _repo.DeleteAsync<GameItem>(match.Id, game.SK);
            Assert.True(deleted);
            games = await _repo.GetItemsAsync<GameItem>(match.Id, "GAME#");
            Assert.NotNull(games);
            Assert.Empty(games);
        }

        [Fact]
        public void GameItemDoesSupportGlobalSearchIndices()
        {
            var gameItemFactory = ItemFactoryCreator.Create<GameItem>();
            Assert.NotNull(gameItemFactory);
            Assert.Equal("MATCH#{0}", gameItemFactory.PKFormat);
            Assert.Equal("GAME#{0}#{1}", gameItemFactory.SKFormat);
            Assert.Equal("GAME#", gameItemFactory.SKPrefix);
            Assert.Equal("PLAYER#{0}", gameItemFactory.GSI1PKFormat);
            Assert.Equal("GAME#{0}#{1}", gameItemFactory.GSI1SKFormat);
            Assert.Equal("GAME#", gameItemFactory.GSI1SKPrefix);
        }

        [Fact]
        public void GameItemConstructsCorrectPrimaryKeys()
        {
            // use stable ids to verify string formatting
            var gameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var playerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var matchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            var item = new GameItem
            {
                Id = gameId,
                PlayerId = playerId,
                MatchId = matchId,
                Result = GameResult.LostResign,

            };

            // pk and sk must follow the factory rules
            var factory = ItemFactoryCreator.Create<GameItem>();

            Assert.Equal(string.Format(factory.PKFormat, matchId), item.PK);
            Assert.StartsWith(factory.SKPrefix, item.SK);
            Assert.Equal(string.Format(factory.SKFormat, gameId, "LOST"), item.SK);
        }

        [Fact]
        public void GameItemConstructsCorrectGsiKeys()
        {
            var item = new GameItem
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                MatchId = Guid.NewGuid(),
                Result = GameResult.Single,
                Modus = GameModus.Portes
            };

            var factory = ItemFactoryCreator.Create<GameItem>();

            // gsi partition key must follow the factory format
            Assert.Equal(
                string.Format(factory.GSI1PKFormat, item.PlayerId),
                item.GSI1PK
            );

            // gsi sort key must follow the factory prefix
            Assert.Equal(
                string.Format(factory.GSI1SKFormat, GameModus.Portes, "WON"),
                item.GSI1SK
            );
        }
    }
}
