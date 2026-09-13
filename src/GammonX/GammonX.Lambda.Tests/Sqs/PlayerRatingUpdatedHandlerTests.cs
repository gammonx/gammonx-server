using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Handlers;

using Moq;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
    public class PlayerRatingUpdatedHandlerTests
    {
        [Fact]
        public async Task RetriesCanceledRatingTransactionWithRecalculation()
        {
            var createAttempts = 0;
            var writeAttempts = 0;

            var result = await PlayerRatingUpdatedHandler.ExecuteRatingUpdateAsync(
                Guid.NewGuid(),
                () => Task.FromResult((false, false)),
                () =>
                {
                    createAttempts++;
                    return Task.FromResult(Array.Empty<DynamoDbPutOperation>());
                },
                _ =>
                {
                    writeAttempts++;
                    return writeAttempts == 1
                        ? Task.FromException(new TransactionCanceledException("revision conflict"))
                        : Task.CompletedTask;
                });

            Assert.True(result);
            Assert.Equal(2, createAttempts);
            Assert.Equal(2, writeAttempts);
        }

        [Fact]
        public async Task TreatsPairedPeriodsAfterCancellationAsDuplicate()
        {
            var stateReads = 0;
            var result = await PlayerRatingUpdatedHandler.ExecuteRatingUpdateAsync(
                Guid.NewGuid(),
                () => Task.FromResult(++stateReads == 1 ? (false, false) : (true, true)),
                () => Task.FromResult(Array.Empty<DynamoDbPutOperation>()),
                _ => Task.FromException(new TransactionCanceledException("duplicate")));

            Assert.False(result);
            Assert.Equal(2, stateReads);
        }

        [Fact]
        public async Task RethrowsAfterThreeCanceledRatingTransactions()
        {
            var createAttempts = 0;
            var writeAttempts = 0;

            await Assert.ThrowsAsync<TransactionCanceledException>(() =>
                PlayerRatingUpdatedHandler.ExecuteRatingUpdateAsync(
                    Guid.NewGuid(),
                    () => Task.FromResult((false, false)),
                    () =>
                    {
                        createAttempts++;
                        return Task.FromResult(Array.Empty<DynamoDbPutOperation>());
                    },
                    _ =>
                    {
                        writeAttempts++;
                        return Task.FromException(new TransactionCanceledException("revision conflict"));
                    }));

            Assert.Equal(3, createAttempts);
            Assert.Equal(3, writeAttempts);
        }

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