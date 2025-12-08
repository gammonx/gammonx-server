using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Lambda.Handlers
{
    /// <summary>variant}/
    /// GET /players/{id}/rating/{variant} > Lambda: GetPlayerRatingHandler
    /// </summary>
    public class GetPlayerRatingHandler : LambdaHandlerBaseImpl, IApiLambdaHandler
    {
        public GetPlayerRatingHandler(IDynamoDbRepository repo) : base(repo)
        {
            // pass
        }


        // <inheritdoc />
        public async Task<BaseResponseContract?> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (_repo == null)
                {
                    context.Logger.LogInformation($"Setting up DI services...");
                    var services = Startup.Configure();
                    _repo = services.GetRequiredService<IDynamoDbRepository>();
                }

                if (_repo == null)
                    throw new NullReferenceException("db repo must not be null");

                var playerIdStr = request.PathParameters["id"];
                var playerId = Guid.Parse(playerIdStr);
                var variantStr = request.PathParameters["variant"];
                var variant = Enum.Parse<MatchVariant>(variantStr);

                var playerRatingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
                var sk = string.Format(playerRatingFactory.SKFormat, variant);
                var ratings = await _repo.GetItemsAsync<PlayerRatingItem>(playerId, sk);

                if (ratings.Count() == 1)
                {
                    var ratingItem =  ratings.First();
                    return ratingItem.ToResponse();
                }
                else if (ratings.Count() == 0)
                {
                    context.Logger.LogInformation($"Create new player rating for Player: '{playerId}' Variant: '{variant}' Type: '{MatchType.SevenPointGame}'");
                    // the player has no rating yet, we create one
                    var newRating = PlayerRatingItemFactory.CreateInitial(playerId, variant, MatchType.SevenPointGame);
                    return newRating.ToResponse();
                }
                else
                {
                    throw new InvalidOperationException($"Multiple player ratings found for the given player '{playerId}' and variant '{variant}'");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex, $"An error occurred while reading player rating. Error '{ex.Message}'.");
                throw;
            }            
        }
    }
}
