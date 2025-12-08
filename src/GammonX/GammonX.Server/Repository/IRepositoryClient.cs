using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Server.Repository
{
    /// <summary>
    /// Provides capabilities to read data from the repository.
    /// </summary>
    public interface IRepositoryClient
    {
        /// <summary>
        /// Gets the base url of the underlying http client.
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Gets the ranked rating for the given <paramref name="playerId"/> and <paramref name="variant"/>.
        /// </summary>
        /// <remarks>
        /// GET /players/{id}/rating/{variant}
        /// </remarks>
        /// <param name="playerId">Player to search for.</param>
        /// <param name="variant">Variant to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An intsance of <see cref="PlayerRatingResponseContract"/>.</returns>
        Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, CancellationToken cancellationToken);
    }
}
