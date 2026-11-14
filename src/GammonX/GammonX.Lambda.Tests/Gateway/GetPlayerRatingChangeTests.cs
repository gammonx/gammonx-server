using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers;
using GammonX.Models.Contracts;

using Moq;

using Xunit;

namespace GammonX.Lambda.Tests.Gateway
{
    public class GetPlayerRatingChangeTests
    {
        [Theory]
        [InlineData(1250, true, 50d)]
        [InlineData(1150, true, -50d)]
        [InlineData(1200, true, 0d)]
        [InlineData(1200, false, null)]
        public async Task ReturnsGainOnlyAfterMatchRatingPeriodIsCommitted(double currentRating, bool committed, double? expectedGain)
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var repo = new Mock<IDynamoDbRepository>(MockBehavior.Strict);
            var reader = repo.As<IDynamoDbConsistentReader>();
            reader.Setup(r => r.GetItemsConsistentlyAsync<RatingPeriodItem>(playerId, $"MATCH#Backgammon#FivePointGame#Ranked#{matchId}"))
                .ReturnsAsync(committed ? new[] { new RatingPeriodItem { PlayerRating = 1200 } } : Array.Empty<RatingPeriodItem>());
            reader.Setup(r => r.GetItemsConsistentlyAsync<PlayerRatingItem>(playerId, "RATING#Backgammon#FivePointGame"))
                .ReturnsAsync(new[] { new PlayerRatingItem { Rating = currentRating } });

            var request = new APIGatewayProxyRequest
            {
                PathParameters = new Dictionary<string, string>
                {
                    ["id"] = playerId.ToString(),
                    ["variant"] = "Backgammon",
                    ["type"] = "FivePointGame"
                },
                QueryStringParameters = new Dictionary<string, string> { ["matchId"] = matchId.ToString() }
            };

            var result = await new GetPlayerRatingHandler(repo.Object).HandleAsync(request, new TestLambdaContext());

            var response = Assert.IsType<PlayerRatingResponseContract>(result);
            Assert.Equal(currentRating, response.Rating);
            Assert.Equal(committed ? (double?)1200 : null, response.PreviousRating);
            Assert.Equal(expectedGain, response.RatingChange);
            reader.VerifyAll();
        }
    }
}
