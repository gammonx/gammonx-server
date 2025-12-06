using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers;
using GammonX.Lambda.Handlers.Contracts;
using GammonX.Lambda.Services;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using Xunit;


namespace GammonX.Lambda.Tests.Gateway
{
    public class GetPlayerRatingHandlerTests
    {
        private readonly IServiceProvider _services;
        private readonly IDynamoDbRepository _repo;

        public GetPlayerRatingHandlerTests()
        {
            _services = Startup.Configure();
            _ = Startup.ConfigureDynamoDbTableAsync(_services);
            _repo = _services.GetRequiredService<IDynamoDbRepository>();
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavli)]
        [InlineData(MatchVariant.Tavla)]
        public async Task OnGetPlayerRatingTests(MatchVariant variant)
        {
            var playerId = Guid.NewGuid();

            var playerRating = new PlayerRatingItem()
            {
                PlayerId = playerId,
                Variant = variant,
                Modus = MatchModus.Ranked,
                Type = Models.Enums.MatchType.SevenPointGame,
                Rating = 1200,
                HighestRating = 1200,
                LowestRating = 1200,
                MatchesPlayed = 1,
                RatingDeviation = 350,
                Sigma = 0.03
            };

            await _repo.SaveAsync(playerRating);

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext
            {
                Logger = logger
            };

            var request = MakeRequest(playerId, variant);

            var handler = LambdaFunctionFactory.CreateApiHandler(request, _services);
            Assert.NotNull(handler);

            var result = await handler.HandleAsync(request, context);

            Assert.NotNull(result);
            var castedRating = result as PlayerRatingResponseContract;
            Assert.NotNull(castedRating);
            Assert.Equal(playerRating.Rating, castedRating.Rating);

            await _repo.DeleteAsync<PlayerRatingItem>(playerRating.PlayerId, playerRating.SK);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavli)]
        [InlineData(MatchVariant.Tavla)]
        public async Task GetPlayerRatingShouldReturnNullWhenMultipleRatingsExist(MatchVariant variant)
        {
            var playerId = Guid.NewGuid();

            var item1 = new PlayerRatingItem
            {
                PlayerId = playerId,
                Variant = variant,
                Modus = MatchModus.Ranked,
                Type = Models.Enums.MatchType.SevenPointGame,
                Rating = 1400,
                HighestRating = 1400,
                LowestRating = 1400,
                MatchesPlayed = 2,
                RatingDeviation = 200,
                Sigma = 0.04
            };

            var item2 = new PlayerRatingItem
            {
                PlayerId = playerId,
                Variant = variant,
                Modus = MatchModus.Ranked,
                Type = Models.Enums.MatchType.SevenPointGame,
                Rating = 1450,
                HighestRating = 1450,
                LowestRating = 1400,
                MatchesPlayed = 2,
                RatingDeviation = 200,
                Sigma = 0.04
            };

            await _repo.SaveAsync(item1);
            await _repo.SaveAsync(item2);

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext { Logger = logger };

            var request = MakeRequest(playerId, variant);

            var handler = LambdaFunctionFactory.CreateApiHandler(request, _services);
            Assert.NotNull(handler);

            var result = await handler.HandleAsync(request, context);

            Assert.NotNull(result);
            Assert.DoesNotContain("Multiple player ratings found", logger.Buffer.ToString());

            await _repo.DeleteAsync<PlayerRatingItem>(item1.PlayerId, item1.SK);
            await _repo.DeleteAsync<PlayerRatingItem>(item2.PlayerId, item2.SK);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavli)]
        [InlineData(MatchVariant.Tavla)]
        public async Task GetPlayerRatingShouldReturnNullWhenPlayerIdIsInvalid(MatchVariant variant)
        {
            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext { Logger = logger };

            var request = new APIGatewayProxyRequest
            {
                Resource = "/players/{id}/{variant}/rating",
                HttpMethod = "GET",
                PathParameters = new Dictionary<string, string>
                {
                    ["id"] = "NOT-A-GUID",
                    ["variant"] = variant.ToString()
                }
            };

            var handler = LambdaFunctionFactory.CreateApiHandler(request, _services);
            Assert.NotNull(handler);

            var result = await handler.HandleAsync(request, context);

            Assert.Null(result);
            Assert.Contains("An error occurred", logger.Buffer.ToString());
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavli)]
        [InlineData(MatchVariant.Tavla)]
        public async Task LambdaFactoryShouldReturnNullOnUnknownAPIRoute(MatchVariant variant)
        {
            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext { Logger = logger };

            var request = new APIGatewayProxyRequest
            {
                Resource = "/players/{id}/{variant}/rating123",
                HttpMethod = "GET",
                PathParameters = new Dictionary<string, string>
                {
                    ["id"] = "NOT-A-GUID",
                    ["variant"] = variant.ToString()
                }
            };

            var handler = LambdaFunctionFactory.CreateApiHandler(request, _services);
            Assert.Null(handler);
        }

        [Fact]
        public async Task GetPlayerRatingShouldReturnNullWhenRepositoryThrows()
        {
            // create local stubbed repo that throws
            var mockRepo = new Mock<IDynamoDbRepository>();
            mockRepo.Setup(x => x.GetItemsAsync<PlayerRatingItem>(It.IsAny<Guid>(), It.IsAny<string>()))
                    .ThrowsAsync(new Exception("Repo failure"));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mockRepo.Object);
            serviceCollection.AddKeyedTransient<IApiLambdaHandler, GetPlayerRatingHandler>(typeof(GetPlayerRatingHandler));

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext { Logger = logger };

            var request = MakeRequest(Guid.NewGuid(), MatchVariant.Backgammon);

            var handler = LambdaFunctionFactory.CreateApiHandler(request, _services);
            Assert.NotNull(handler);

            var result = await handler.HandleAsync(request, context);

            Assert.Null(result);
            Assert.Contains("Error: An error occurred while reading", logger.Buffer.ToString());
        }

        private static APIGatewayProxyRequest MakeRequest(Guid playerId, MatchVariant variant)
        {
            return new APIGatewayProxyRequest
            {
                Resource = "/players/{id}/{variant}/rating",
                HttpMethod = "GET",
                PathParameters = new Dictionary<string, string>
                {
                    ["id"] = playerId.ToString(),
                    ["variant"] = variant.ToString()
                }
            };
        }
    }
}
