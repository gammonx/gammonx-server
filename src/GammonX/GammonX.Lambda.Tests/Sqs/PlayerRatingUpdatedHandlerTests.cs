using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Handlers;

using Moq;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
    public class PlayerRatingUpdatedHandlerTests
    {
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, true, true)]
        public async Task ClassifiesCanceledTransactionAsDuplicateOnlyWhenBothPeriodsExist(
            bool winnerPeriodExists,
            bool loserPeriodExists,
            bool expected)
        {
            var winnerId = Guid.NewGuid();
            var loserId = Guid.NewGuid();
            const string periodSk = "MATCH#Backgammon#FivePointGame#Ranked#match";
            var repository = new Mock<IDynamoDbRepository>();
            repository
                .Setup(value => value.GetItemsAsync<RatingPeriodItem>(winnerId, periodSk))
                .ReturnsAsync(CreatePeriods(winnerPeriodExists));
            repository
                .Setup(value => value.GetItemsAsync<RatingPeriodItem>(loserId, periodSk))
                .ReturnsAsync(CreatePeriods(loserPeriodExists));

            var result = await PlayerRatingUpdatedHandler.IsDuplicateTransactionAsync(
                repository.Object,
                winnerId,
                loserId,
                periodSk);

            Assert.Equal(expected, result);
        }

        private static IEnumerable<RatingPeriodItem> CreatePeriods(bool exists)
        {
            return exists ? [new RatingPeriodItem()] : [];
        }
    }
}