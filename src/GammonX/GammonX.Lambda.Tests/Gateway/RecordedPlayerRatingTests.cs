using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Moq;

using Xunit;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Lambda.Tests.Gateway;

public class RecordedPlayerRatingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RatingResponseDistinguishesStored1200FromInitial1200(bool stored)
    {
        // Arrange.
        var playerId = Guid.NewGuid();
        var repository = new Mock<IDynamoDbRepository>();
        var item = new PlayerRatingItem
        {
            PlayerId = playerId,
            Variant = MatchVariant.Backgammon,
            Type = MatchType.FivePointGame,
            Modus = MatchModus.Ranked,
            Rating = 1200
        };
        repository.Setup(value => value.GetItemsAsync<PlayerRatingItem>(playerId, "RATING#Backgammon#FivePointGame"))
            .ReturnsAsync(stored ? new[] { item } : Array.Empty<PlayerRatingItem>());
        var handler = new GetPlayerRatingHandler(repository.Object);
        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                ["id"] = playerId.ToString(),
                ["variant"] = "Backgammon",
                ["type"] = "FivePointGame"
            }
        };

        // Act.
        var result = await handler.HandleAsync(request, new TestLambdaContext());

        // Assert.
        var response = Assert.IsType<PlayerRatingResponseContract>(result);
        Assert.Equal(1200, response.Rating);
        Assert.Equal(stored, response.HasRecordedRating);
    }
}
