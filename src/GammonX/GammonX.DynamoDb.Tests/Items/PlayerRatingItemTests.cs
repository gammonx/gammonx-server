using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Services;
using GammonX.DynamoDb.Stats;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class PlayerRatingItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public PlayerRatingItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Theory]
        [InlineData("RATING#Backgammon#SevenPointGame", MatchVariant.Backgammon)]
        [InlineData("RATING#Tavla#SevenPointGame", MatchVariant.Tavla)]
        [InlineData("RATING#Tavli#SevenPointGame", MatchVariant.Tavli)]
        public async Task CanCreateAndSearchMultipleRatingsPerPlayer(string oneSk, MatchVariant expVariant)
        {
            var player = ItemFactory.CreatePlayer();
            // create all combinations of player ratings (3 total)
            var allRatings = ItemFactory.CreateAllPlayerRatings(player);
            // create them all!
            foreach (var rating in allRatings)
            {
                await _repo.SaveAsync(rating);
            }
            // read and query them
            var all = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id);
            Assert.NotNull(all);
            Assert.Equal(3, all.Count());
            var one = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id, oneSk);
            Assert.NotNull(one);
            Assert.Single(one);
            Assert.Equal(expVariant, one.First().Variant);
            Assert.Equal(MatchType.SevenPointGame, one.First().Type);
            Assert.Equal(MatchModus.Ranked, one.First().Modus);
            // delete them all!
            foreach (var rating in allRatings)
            {
                await _repo.DeleteAsync<PlayerRatingItem>(rating.PlayerId, rating.SK);
            }
        }

        [Fact]
        public async Task CreateSearchUpdateDeletePlayerRatings()
        {
            var player = ItemFactory.CreatePlayer();
            var playerRatings = ItemFactory.CreatePlayerRating(player, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var factory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var attributes = factory.CreateItem(playerRatings);
            Assert.True(attributes.ContainsKey("PlayerId"));
            Assert.False(attributes.ContainsKey("Id"));
            // create
            await _repo.SaveAsync(playerRatings);
            // read
            var ratings = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id);
            Assert.NotNull(ratings);
            Assert.Single(ratings);
            var ratingFromRepo = ratings.First();
            Assert.Equal($"PLAYER#{player.Id}", ratingFromRepo.PK);
            Assert.Equal("RATING#Backgammon#SevenPointGame", ratingFromRepo.SK);
            Assert.Equal(player.Id, ratingFromRepo.PlayerId);
            Assert.Equal(ItemTypes.PlayerRatingItemType, ratingFromRepo.ItemType);
            Assert.Equal(MatchVariant.Backgammon, ratingFromRepo.Variant);
            Assert.Equal(MatchModus.Ranked, ratingFromRepo.Modus);
            Assert.Equal(MatchType.SevenPointGame, ratingFromRepo.Type);
            Assert.Equal(Glicko2Constants.DefaultRating, ratingFromRepo.Rating);
            Assert.Equal(Glicko2Constants.DefaultRD, ratingFromRepo.RatingDeviation);
            Assert.Equal(Glicko2Constants.DefaultSigma, ratingFromRepo.Sigma);
            Assert.Equal(1800, ratingFromRepo.HighestRating);
            Assert.Equal(1000, ratingFromRepo.LowestRating);
            Assert.Equal(30, ratingFromRepo.MatchesPlayed);
            // update
            ratingFromRepo.MatchesPlayed++;
            await _repo.SaveAsync(ratingFromRepo);
            ratings = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id);
            Assert.NotNull(ratings);
            Assert.Single(ratings);
            ratingFromRepo = ratings.First();
            Assert.Equal(31, ratingFromRepo.MatchesPlayed);
            // delete
            var deleted = await _repo.DeleteAsync<PlayerRatingItem>(player.Id, "RATING#Backgammon#SevenPointGame");
            Assert.True(deleted);
            ratings = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id);
            Assert.NotNull(ratings);
            Assert.Empty(ratings);
        }

        [Fact]
        public void PlayerRatingsItemDoesNotSupportGlobalSearchIndices()
        {
            var playerItemFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            Assert.NotNull(playerItemFactory);
            Assert.Equal("PLAYER#{0:D}", playerItemFactory.PKFormat);
            Assert.Equal("RATING#{0}#{1}", playerItemFactory.SKFormat);
            Assert.Equal("RATING#", playerItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKPrefix);
        }

        [Fact]
        public async Task PlayerRatingSortKeyIncludesMatchType()
        {
            var player = ItemFactory.CreatePlayer();
            var sevenPointRating = ItemFactory.CreatePlayerRating(player, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var fivePointRating = ItemFactory.CreatePlayerRating(player, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame);

            await _repo.SaveAsync(sevenPointRating);
            await _repo.SaveAsync(fivePointRating);

            var ratings = (await _repo.GetItemsAsync<PlayerRatingItem>(player.Id)).ToList();
            Assert.Equal(2, ratings.Count);
            Assert.Contains(ratings, rating => rating.SK == "RATING#Backgammon#SevenPointGame");
            Assert.Contains(ratings, rating => rating.SK == "RATING#Backgammon#FivePointGame");

            await _repo.DeleteAsync<PlayerRatingItem>(player.Id, sevenPointRating.SK);
            await _repo.DeleteAsync<PlayerRatingItem>(player.Id, fivePointRating.SK);
        }

        [Fact]
        public async Task NonRankedRatingUpdateIsRejected()
        {
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, opponent, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _repo.CalculatePlayerRatingAsync(player.Id, wonMatch, lostMatch));

            Assert.Contains("Ranked", exception.Message);
        }
    }
}
