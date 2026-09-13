using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda.Handlers
{
    /// <summary>
    /// GET /players/{id}/games > Lambda: GetPlayerGamesHandler
    /// </summary>
    public class GetPlayerGamesHandler : LambdaHandlerBaseImpl, IApiLambdaHandler
    {
        public GetPlayerGamesHandler(IDynamoDbRepository repo) : base(repo)
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

                var games = await Repo.GetItemsByGSIPKAsync<GameItem>(playerId);

                return games.ToGamesResponse();
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex, $"An error occurred while reading player history. Error '{ex.Message}'.");
                throw;
            }
        }
    }
}
