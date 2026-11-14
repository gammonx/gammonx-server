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
    /// <summary>
    /// GET /players/{id}/rating/{variant}/{type} > Lambda: GetPlayerRatingHandler
    /// </summary>
    /// <remarks>
    /// The optional matchId query parameter selects that matchs stored pre-match rating as the
    /// comparison baseline. PreviousRating and RatingChange remain null until its period is committed.
    /// RatingChange is the current rating minus that baseline, so it includes any later rating updates.
    /// This endpoint reads stored data and does not trigger rating processing.
    /// </remarks>
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
                if (Repo == null)
                {
                    context.Logger.LogInformation("Setting up DI services...");
                    var services = Startup.Configure();
                    Repo = services.GetRequiredService<IDynamoDbRepository>();
                }

                if (Repo == null)
                    throw new NullReferenceException("db repo must not be null");

                var playerIdStr = request.PathParameters["id"];
                var playerId = Guid.Parse(playerIdStr);
                var variantStr = request.PathParameters["variant"];
                var variant = Enum.Parse<MatchVariant>(variantStr);
                var typeStr = request.PathParameters["type"];
                var type = Enum.Parse<MatchType>(typeStr);

                RatingPeriodItem? previousPeriod = null;
                // This matchs period stores its pre-match rating. Selecting an earlier match would go back too far.
                var calcRatingChange = request.QueryStringParameters?.ContainsKey("matchId") == true;
                if (calcRatingChange)
                {
                    var matchId = Guid.Parse(request.QueryStringParameters!["matchId"]);
                    var periodFactory = ItemFactoryCreator.Create<RatingPeriodItem>();
                    var periodSk = string.Format(periodFactory.SKFormat, variant, type, MatchModus.Ranked, matchId);
                    var periods = Repo is IDynamoDbConsistentReader periodReader
                        ? await periodReader.GetItemsConsistentlyAsync<RatingPeriodItem>(playerId, periodSk)
                        : await Repo.GetItemsAsync<RatingPeriodItem>(playerId, periodSk);
                    previousPeriod = periods.SingleOrDefault();
                }

                var playerRatingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
                var sk = string.Format(playerRatingFactory.SKFormat, variant, type);
                var ratings = calcRatingChange && Repo is IDynamoDbConsistentReader ratingReader
                    ? await ratingReader.GetItemsConsistentlyAsync<PlayerRatingItem>(playerId, sk)
                    : await Repo.GetItemsAsync<PlayerRatingItem>(playerId, sk);

                if (ratings.Count() == 1)
                {
                    var ratingItem =  ratings.First();
                    var response = ratingItem.ToResponse();

                    if (previousPeriod != null)
                    {
                        response.PreviousRating = previousPeriod.PlayerRating;
                        response.RatingChange = ratingItem.Rating - previousPeriod.PlayerRating;
                    }
                    else if (calcRatingChange && previousPeriod == null)
                    {
                        context.Logger.LogWarning($"Calculating the rating change for Player '{playerId}' was requested but not yet executable.");
                    }
                    
                    return response;
                }
                else if (!ratings.Any())
                {
                    context.Logger.LogInformation($"Create new player rating for Player: '{playerId}' Variant: '{variant}' Type: '{type}'");
                    // the player has no rating yet, we create one
                    var newRating = PlayerRatingItemFactory.CreateInitial(playerId, variant, type);
                    return newRating.ToResponse();
                }
                else
                {
                    throw new InvalidOperationException($"Multiple player ratings found for player '{playerId}', variant '{variant}', and type '{type}'");
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
