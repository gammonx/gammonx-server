using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Lambda.Handlers
{
    /// <summary>variant}/
    /// GET /players/{id}/{variant}/rating > Lambda: GetPlayerRatingHandler
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
                    throw new InvalidOperationException($"None player rating found for the given player '{playerId}' and variant '{variant}'");
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
