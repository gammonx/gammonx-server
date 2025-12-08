using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Server.Repository
{
    // <inheritdoc />
    public class SimpleRepositoryClient : IRepositoryClient
    {
        // <inheritdoc />
        public string BaseUrl => "dummy-service";

        // <inheritdoc />
        public Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, CancellationToken cancellationToken)
        {
            var dummyRating = new PlayerRatingResponseContract() { Rating = 1200 };
            return Task.FromResult(dummyRating ?? null);
        }
    }
}
