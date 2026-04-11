using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
    // <inheritdoc />
    public class MarsBotService : IBotService
    {
        private readonly HttpClient _httpClient;

        public MarsBotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // <inheritdoc />
        public Task<MoveSequenceModel> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
        {
            throw new NotImplementedException();
        }

        // <inheritdoc />
        public Task<bool> ShouldAcceptDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            throw new InvalidOperationException("Mars bot does not support accepting doubles.");
        }

        // <inheritdoc />
        public Task<bool> ShouldOfferDouble(IMatchSessionModel matchSession, Guid playerId)
        {
            throw new InvalidOperationException("Mars bot does not support accepting doubles.");
        }
    }
}
