using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Repository
{
    // <inheritdoc />
    public class SimpleRepositoryClient : IRepositoryClient
    {
        // <inheritdoc />
        public string BaseUrl => "dummy-service";

        // <inheritdoc />
        public Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, MatchType type, CancellationToken cancellationToken)
        {
            var dummyRating = new PlayerRatingResponseContract { Rating = 1200 };
            return Task.FromResult<PlayerRatingResponseContract?>(dummyRating);
        }

        // <inheritdoc />
        public Task<PlayerGamesResponseContract?> GetPlayersGames(Guid playerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<PlayerGamesResponseContract?>(null);
        }
    }
}
