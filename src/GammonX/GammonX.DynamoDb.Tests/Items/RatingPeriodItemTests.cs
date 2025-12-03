using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Stats;
using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class RatingPeriodItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public RatingPeriodItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateAndSearchMultipleRatingPeriods()
        {
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();

            var match1 = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            var match2 = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            var match3 = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);

            var match4 = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);

            var ratingPeriod1 = ItemFactory.CrateRatingPeriod(player, opponent, match1);
            await _repo.SaveAsync(ratingPeriod1);
            var ratingPeriod2 = ItemFactory.CrateRatingPeriod(player, opponent, match2);
            await _repo.SaveAsync(ratingPeriod2);
            var ratingPeriod3 = ItemFactory.CrateRatingPeriod(player, opponent, match3);
            await _repo.SaveAsync(ratingPeriod3);
            var ratingPeriod4 = ItemFactory.CrateRatingPeriod(player, opponent, match4);
            await _repo.SaveAsync(ratingPeriod4);

            var backgammonPeriods = await _repo.GetItemsAsync<RatingPeriodItem>(player.Id, "MATCH#Backgammon");
            Assert.NotNull(backgammonPeriods);
            Assert.Equal(3, backgammonPeriods.Count());

            var tavliPeriods = await _repo.GetItemsAsync<RatingPeriodItem>(player.Id, "MATCH#Tavli");
            Assert.NotNull(tavliPeriods);
            Assert.Single(tavliPeriods);

            await _repo.DeleteAsync<RatingPeriodItem>(player.Id, ratingPeriod1.SK);
            await _repo.DeleteAsync<RatingPeriodItem>(player.Id, ratingPeriod2.SK);
            await _repo.DeleteAsync<RatingPeriodItem>(player.Id, ratingPeriod3.SK);
            await _repo.DeleteAsync<RatingPeriodItem>(player.Id, ratingPeriod4.SK);
        }

        [Fact]
        public async Task CreateSearchUpdateDeleteRatingPeriod()
        {
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            var match = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame);
            var ratingPeriod = ItemFactory.CrateRatingPeriod(player, opponent, match);
            // create
            await _repo.SaveAsync(ratingPeriod);
            // read
            var periods = await _repo.GetItemsAsync<RatingPeriodItem>(player.Id);
            Assert.NotNull(periods);
            Assert.Single(periods);
            var periodFromRepo = periods.First();
            Assert.Equal(player.Id, periodFromRepo.PlayerId);
            Assert.Equal(match.Id, periodFromRepo.MatchId);
            Assert.Equal(Glicko2Constants.DefaultRating, periodFromRepo.PlayerRating);
            Assert.Equal(Glicko2Constants.DefaultRD, periodFromRepo.PlayerRatingDeviation);
            Assert.Equal(Glicko2Constants.DefaultSigma, periodFromRepo.PlayerSigma);
            Assert.Equal(opponent.Id, periodFromRepo.OpponentId);
            Assert.Equal(Glicko2Constants.DefaultRating, periodFromRepo.OpponentRating);
            Assert.Equal(Glicko2Constants.DefaultRD, periodFromRepo.OpponentRatingDeviation);
            Assert.Equal(Glicko2Constants.DefaultSigma, periodFromRepo.OpponentSigma);
            Assert.Equal(ItemTypes.RatingPeriodItemType, periodFromRepo.ItemType);
            Assert.Equal(1, periodFromRepo.MatchScore);
            Assert.Equal(MatchVariant.Backgammon, periodFromRepo.Variant);
            Assert.Equal(MatchModus.Normal, periodFromRepo.Modus);
            Assert.Equal(MatchType.CashGame, periodFromRepo.Type);
            Assert.Equal($"PLAYER#{player.Id}", periodFromRepo.PK);
            Assert.Equal($"MATCH#Backgammon#CashGame#Normal#{match.Id}", periodFromRepo.SK);
            // update
            periodFromRepo.PlayerRating += 100;
            await _repo.SaveAsync(periodFromRepo);
            periods = await _repo.GetItemsAsync<RatingPeriodItem>(player.Id);
            Assert.NotNull(periods);
            Assert.Single(periods);
            periodFromRepo = periods.First();
            Assert.Equal(Glicko2Constants.DefaultRating + 100, periodFromRepo.PlayerRating);
            // delete
            var deleted = await _repo.DeleteAsync<RatingPeriodItem>(player.Id, periodFromRepo.SK);
            Assert.True(deleted);
            periods = await _repo.GetItemsAsync<RatingPeriodItem>(player.Id);
            Assert.NotNull(periods);
            Assert.Empty(periods);
        }

        [Fact]
        public void RatingPeriodItemDoesNotSupportGlobalSearchIndices()
        {
            var ratingPeriodFactory = ItemFactoryCreator.Create<RatingPeriodItem>();
            Assert.NotNull(ratingPeriodFactory);
            Assert.Equal("PLAYER#{0}", ratingPeriodFactory.PKFormat);
            Assert.Equal("MATCH#{0}#{1}#{2}#{3}", ratingPeriodFactory.SKFormat);
            Assert.Equal("MATCH#", ratingPeriodFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => ratingPeriodFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => ratingPeriodFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => ratingPeriodFactory.GSI1SKPrefix);
        }
    }
}
