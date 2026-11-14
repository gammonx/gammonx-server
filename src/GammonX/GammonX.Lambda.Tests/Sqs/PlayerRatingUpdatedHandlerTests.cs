using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.DynamoDb;
using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Handlers;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Microsoft.Extensions.Options;

using Moq;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
    public class PlayerRatingUpdatedHandlerTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PairedRatingTransactionUsesMatchingPreMatchSnapshotsWhenOpponentChangesBetweenReads(bool concurrentUpdate)
        {
            // Arrange: another match updates the loser after the winner calculation reads that opponent.
            var winnerId = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var loserId = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");
            var matchId = Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e");
            var history = await File.ReadAllTextAsync(Path.Combine("Data", "TavliMatchHistory.txt"), TestContext.Current.CancellationToken);
            var work = new RatingUpdateWorkContract
            {
                Records = new[] { (winnerId, MatchResult.Won), (loserId, MatchResult.Lost) }
                    .Select(player => new MatchRecordContract
                    {
                        Id = matchId,
                        PlayerId = player.Item1,
                        Result = player.Item2,
                        Variant = MatchVariant.Tavli,
                        Modus = MatchModus.Ranked,
                        Type = Models.Enums.MatchType.SevenPointGame,
                        Format = HistoryFormat.MAT,
                        MatchHistory = history
                    }).ToArray()
            };
            var loserReads = 0;
            var currentLoserRevision = 7;
            var ratingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var client = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
            client.Setup(db => db.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((QueryRequest request, CancellationToken _) =>
                {
                    Assert.True(request.ConsistentRead);
                    if (request.ExpressionAttributeValues[":skPrefix"].S.StartsWith("MATCH#", StringComparison.Ordinal))
                    {
                        return new QueryResponse { Items = [], LastEvaluatedKey = [] };
                    }

                    var isLoser = request.ExpressionAttributeValues[":pk"].S == $"PLAYER#{loserId}";
                    if (isLoser)
                    {
                        loserReads++;
                    }
                    var rating = PlayerRatingItemFactory.CreateInitial(isLoser ? loserId : winnerId, MatchVariant.Tavli, Models.Enums.MatchType.SevenPointGame);
                    rating.Rating = isLoser ? (currentLoserRevision == 7 ? 1200 : 1400) : 1300;
                    rating.Revision = isLoser ? currentLoserRevision : 5;
                    var attributes = ratingFactory.CreateItem(rating);
                    if (isLoser && loserReads == 1 && concurrentUpdate)
                    {
                        // Commit another match after this snapshot was read, before the transaction writes it.
                        currentLoserRevision = 8;
                    }
                    return new QueryResponse { Items = [attributes], LastEvaluatedKey = [] };
                });
            TransactWriteItemsRequest? transaction = null;
            var writeAttempts = 0;
            client.Setup(db => db.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TransactWriteItemsRequest request, CancellationToken _) =>
                {
                    writeAttempts++;
                    foreach (var put in request.TransactItems.Select(item => item.Put).Where(put => put.Item["ItemType"].S == "PlayerRating"))
                    {
                        var expectedRevision = put.Item["PlayerId"].S == loserId.ToString() ? currentLoserRevision : 5;
                        if (put.ExpressionAttributeValues[":expectedRevision"].N != expectedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        {
                            throw new TransactionCanceledException("revision conflict");
                        }
                    }
                    transaction = request;
                    return new TransactWriteItemsResponse();
                });
            var repo = new DynamoDbRepository(client.Object, new Mock<IDynamoDBContext>().Object,
                Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" }));
            var sqsEvent = new SQSEvent
            {
                Records = [new SQSEvent.SQSMessage { MessageId = "rating-update", Body = JsonConvert.SerializeObject(work) }]
            };

            // Act.
            var response = await new PlayerRatingUpdatedHandler(repo).HandleAsync(sqsEvent, new TestLambdaContext());

            // Assert: revision checks must not allow the two periods to describe different starting ratings.
            Assert.Empty(response.BatchItemFailures);
            Assert.NotNull(transaction);
            var periods = transaction.TransactItems.Select(item => item.Put.Item)
                .Where(item => item["ItemType"].S == "RatingPeriod").ToArray();
            var winnerPeriod = Assert.Single(periods, item => item["PlayerId"].S == winnerId.ToString());
            var loserPeriod = Assert.Single(periods, item => item["PlayerId"].S == loserId.ToString());
            Assert.Equal(loserPeriod["PlayerRating"].N, winnerPeriod["OpponentRating"].N);
            Assert.Equal(winnerPeriod["PlayerRating"].N, loserPeriod["OpponentRating"].N);
            Assert.Equal(concurrentUpdate ? "1400" : "1200", loserPeriod["PlayerRating"].N);
            Assert.Equal(concurrentUpdate ? 2 : 1, loserReads);
            Assert.Equal(concurrentUpdate ? 2 : 1, writeAttempts);
            client.Verify(db => db.QueryAsync(
                It.Is<QueryRequest>(request => request.ExpressionAttributeValues[":skPrefix"].S.StartsWith("RATING#", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()), Times.Exactly(concurrentUpdate ? 4 : 2));
        }

        [Fact]
        public async Task AlreadyCommittedMatchSkipsCalculationAndWrite()
        {
            // Arrange.
            var createAttempts = 0;
            var writeAttempts = 0;

            // Act.
            var updated = await PlayerRatingUpdatedHandler.ExecuteRatingUpdateAsync(
                Guid.NewGuid(),
                () => Task.FromResult((true, true)),
                () => { createAttempts++; return Task.FromResult(Array.Empty<DynamoDbPutOperation>()); },
                _ => { writeAttempts++; return Task.CompletedTask; });

            // Assert.
            Assert.False(updated);
            Assert.Equal(0, createAttempts);
            Assert.Equal(0, writeAttempts);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task UnpairedPeriodsRejectUpdateBeforeCalculation(bool winnerExists, bool loserExists)
        {
            // Arrange.
            var matchId = Guid.NewGuid();

            // Act.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PlayerRatingUpdatedHandler.ExecuteRatingUpdateAsync(
                    matchId,
                    () => Task.FromResult((winnerExists, loserExists)),
                    () => throw new Xunit.Sdk.XunitException("Calculation must not run for inconsistent periods."),
                    _ => throw new Xunit.Sdk.XunitException("Write must not run for inconsistent periods.")));

            // Assert.
            Assert.Contains(matchId.ToString(), exception.Message);
        }

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
