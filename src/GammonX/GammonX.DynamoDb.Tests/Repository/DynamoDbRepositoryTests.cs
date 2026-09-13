using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Models.Enums;

using Microsoft.Extensions.Options;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Repository
{
    public class DynamoDbRepositoryTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task QueryMethodsReadAllPages(bool useGlobalIndex, bool useExplicitSortKey)
        {
            var ownerId = Guid.NewGuid();
            var firstMatch = CreateMatch(ownerId);
            var secondMatch = CreateMatch(ownerId);
            var factory = ItemFactoryCreator.Create<MatchItem>();

            var continuationKey = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue(firstMatch.PK) },
                { "SK", new AttributeValue(firstMatch.SK) }
            };

            var responses = new Queue<QueryResponse>(
            [
                new QueryResponse
                {
                    Items = [factory.CreateItem(firstMatch)],
                    LastEvaluatedKey = continuationKey
                },
                new QueryResponse
                {
                    Items = [factory.CreateItem(secondMatch)],
                    LastEvaluatedKey = []
                }
            ]);

            var exclusiveStartKeys = new List<Dictionary<string, AttributeValue>?>();
            var client = new Mock<IAmazonDynamoDB>();

            client
                .Setup(value => value.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .Callback<QueryRequest, CancellationToken>((request, _) => exclusiveStartKeys.Add(request.ExclusiveStartKey))
                .ReturnsAsync(() => responses.Dequeue());

            var context = new Mock<IDynamoDBContext>();
            var options = Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" });
            var repository = new DynamoDbRepository(client.Object, context.Object, options);

            IEnumerable<MatchItem> result;

            if (useGlobalIndex)
            {
                result = useExplicitSortKey
                    ? await repository.GetItemsByGSIPKAsync<MatchItem>(ownerId, factory.GSI1SKPrefix)
                    : await repository.GetItemsByGSIPKAsync<MatchItem>(ownerId);
            }
            else
            {
                result = useExplicitSortKey
                    ? await repository.GetItemsAsync<MatchItem>(firstMatch.Id, factory.SKPrefix)
                    : await repository.GetItemsAsync<MatchItem>(firstMatch.Id);
            }

            Assert.Equal([firstMatch.Id, secondMatch.Id], result.Select(item => item.Id));
            Assert.Equal(2, exclusiveStartKeys.Count);
            Assert.Null(exclusiveStartKeys[0]);
            Assert.Same(continuationKey, exclusiveStartKeys[1]);
            client.Verify(value => value.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ConsistentReadSetsQueryConsistency()
        {
            QueryRequest? capturedRequest = null;
            var client = new Mock<IAmazonDynamoDB>();
            client
                .Setup(value => value.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
                .Callback<QueryRequest, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new QueryResponse { Items = [], LastEvaluatedKey = [] });
            
            var context = new Mock<IDynamoDBContext>();
            var options = Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" });
            var repository = new DynamoDbRepository(client.Object, context.Object, options);

            await repository.GetItemsConsistentlyAsync<PlayerRatingItem>(Guid.NewGuid(), "RATING#Backgammon#SevenPointGame");

            Assert.NotNull(capturedRequest);
            Assert.True(capturedRequest.ConsistentRead);
        }

        [Fact]
        public async Task BatchDeleteChunksRequestsAndRetriesOnlyUnprocessedItems()
        {
            var client = new Mock<IAmazonDynamoDB>();
            var requestSizes = new List<int>();
            var call = 0;

            client
                .Setup(value => value.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BatchWriteItemRequest request, CancellationToken _) =>
                {
                    call++;
                    var writes = request.RequestItems["GammonX"];
                    requestSizes.Add(writes.Count);
                    return new BatchWriteItemResponse
                    {
                        UnprocessedItems = call == 1
                            ? new Dictionary<string, List<WriteRequest>> { { "GammonX", [writes[0]] } }
                            : new Dictionary<string, List<WriteRequest>>()
                    };
                });

            var context = new Mock<IDynamoDBContext>();
            var options = Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" });
            var repository = new DynamoDbRepository(client.Object, context.Object, options);
            var keys = Enumerable.Range(0, 30).Select(index => (PkId: Guid.NewGuid(), Sk: $"DETAILS#{index}"));

            await repository.BatchDeleteAsync<MatchItem>(keys);

            Assert.Equal([25, 1, 5], requestSizes);
            client.Verify(value => value.BatchWriteItemAsync(It.IsAny<BatchWriteItemRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task TransactPutSerializesTypedItemsAndConditions()
        {
            TransactWriteItemsRequest? capturedRequest = null;
            var client = new Mock<IAmazonDynamoDB>();
            client
                .Setup(value => value.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
                .Callback<TransactWriteItemsRequest, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new TransactWriteItemsResponse());
            
            var context = new Mock<IDynamoDBContext>();
            var options = Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" });
            var repository = new DynamoDbRepository(client.Object, context.Object, options);

            var playerId = Guid.NewGuid();
            var rating = PlayerRatingItemFactory.CreateInitial(playerId, MatchVariant.Backgammon, MatchType.SevenPointGame);
            rating.Revision = 4;
            var player = new PlayerItem
            {
                Id = playerId,
                UserName = "player",
                CreatedAt = DateTime.UtcNow
            };

            var operations = new[]
            {
                DynamoDbPutOperation.CreateVersioned(rating, 3),
                DynamoDbPutOperation.Create(player)
            };

            await repository.TransactPutAsync(operations);

            Assert.NotNull(capturedRequest);
            Assert.Equal(2, capturedRequest.TransactItems.Count);
            
            var ratingPut = capturedRequest.TransactItems[0].Put;
            
            Assert.Equal("GammonX", ratingPut.TableName);
            Assert.Equal("4", ratingPut.Item["Revision"].N);
            Assert.Equal("attribute_not_exists(#revision) OR #revision = :expectedRevision", ratingPut.ConditionExpression);
            Assert.Equal("Revision", ratingPut.ExpressionAttributeNames["#revision"]);
            Assert.Equal("3", ratingPut.ExpressionAttributeValues[":expectedRevision"].N);
            Assert.Equal("player", capturedRequest.TransactItems[1].Put.Item["Username"].S);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task TransactPutRejectsInvalidOperationCounts(int operationCount)
        {
            var client = new Mock<IAmazonDynamoDB>();
            var context = new Mock<IDynamoDBContext>();
            var options = Options.Create(new DynamoDbOptions { DYNAMODB_TABLENAME = "GammonX" });
            var repository = new DynamoDbRepository(client.Object, context.Object, options);
            var operation = DynamoDbPutOperation.Create(CreateMatch(Guid.NewGuid()));
            var operations = Enumerable.Repeat(operation, operationCount);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.TransactPutAsync(operations));

            client.Verify(value => value.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static MatchItem CreateMatch(Guid playerId)
        {
            return new MatchItem
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Points = 7,
                Length = 5,
                Variant = MatchVariant.Backgammon,
                Modus = MatchModus.Ranked,
                Type = MatchType.SevenPointGame,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                EndedAt = DateTime.UtcNow,
                Result = MatchResult.Won
            };
        }
    }
}