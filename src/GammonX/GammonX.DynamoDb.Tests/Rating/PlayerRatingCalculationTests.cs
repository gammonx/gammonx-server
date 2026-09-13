using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Services;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Rating
{
    public class PlayerRatingCalculationTests
    {
        [Fact]
        public async Task CalculatePlayerRatingUsesOnlyCurrentMatchPeriod()
        {
            var player = ItemFactory.CreatePlayer();
            var opponent = ItemFactory.CreatePlayer();
            
            var matchId = Guid.NewGuid();
            var wonMatch = ItemFactory.CreateMatch(matchId, player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            var lostMatch = ItemFactory.CreateMatch(matchId, opponent, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame);
            
            var playerRating = PlayerRatingItemFactory.CreateInitial( player.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            var opponentRating = PlayerRatingItemFactory.CreateInitial( opponent.Id, MatchVariant.Backgammon, MatchType.SevenPointGame);
            opponentRating.Rating = 1350;
            
            var ratingSk = playerRating.SK;
            var repository = new Mock<IDynamoDbRepository>();
            var consistentReader = repository.As<IDynamoDbConsistentReader>();

            consistentReader
                .Setup(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(player.Id, ratingSk))
                .ReturnsAsync([playerRating]);
            
            consistentReader
                .Setup(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(opponent.Id, ratingSk))
                .ReturnsAsync([opponentRating]);

            var (updatedRating, period) = await repository.Object.CalculatePlayerRatingAsync( player.Id, wonMatch, lostMatch);

            Assert.Equal(1, updatedRating.MatchesPlayed);
            Assert.Equal(1, updatedRating.Revision);
            Assert.True(updatedRating.Rating > 1200);
            Assert.Equal(1200, period.PlayerRating);
            Assert.Equal(1350, period.OpponentRating);
            Assert.Equal(matchId, period.MatchId);
            consistentReader.Verify(value => value.GetItemsConsistentlyAsync<PlayerRatingItem>(It.IsAny<Guid>(), ratingSk), Times.Exactly(2));
            repository.Verify(value => value.GetItemsAsync<PlayerRatingItem>(It.IsAny<Guid>(), ratingSk), Times.Never);
            repository.Verify(value => value.GetItemsAsync<RatingPeriodItem>(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
    }
}