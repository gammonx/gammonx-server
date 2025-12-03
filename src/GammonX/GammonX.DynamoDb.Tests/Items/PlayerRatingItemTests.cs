using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
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
        [InlineData("RATING#Backgammon", MatchVariant.Backgammon)]
        [InlineData("RATING#Tavla", MatchVariant.Tavla)]
        [InlineData("RATING#Tavli", MatchVariant.Tavli)]
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
            // create
            await _repo.SaveAsync(playerRatings);
            // read
            var ratings = await _repo.GetItemsAsync<PlayerRatingItem>(player.Id);
            Assert.NotNull(ratings);
            Assert.Single(ratings);
            var ratingFromRepo = ratings.First();
            Assert.Equal($"PLAYER#{player.Id}", ratingFromRepo.PK);
            Assert.Equal($"RATING#Backgammon", ratingFromRepo.SK);
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
            var deleted = await _repo.DeleteAsync<PlayerRatingItem>(player.Id, "RATING#Backgammon");
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
            Assert.Equal("PLAYER#{0}", playerItemFactory.PKFormat);
            Assert.Equal("RATING#{0}", playerItemFactory.SKFormat);
            Assert.Equal("RATING#", playerItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKPrefix);
        }
    }
}
