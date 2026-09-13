using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

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
        /// Gets the ranked rating for the given player, variant, and match type.
        /// </summary>
        /// <remarks>
        /// GET /players/{id}/rating/{variant}/{type}
        /// </remarks>
        /// <param name="playerId">Player to search for.</param>
        /// <param name="variant">Variant to search for.</param>
        /// <param name="type">Match type to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An instance of <see cref="PlayerRatingResponseContract"/>.</returns>
        Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, MatchType type, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the games for the given <paramref name="playerId"/>.
        /// </summary>
        /// <remarks>
        /// GET /players/{id}/games
        /// </remarks>
        /// <param name="playerId">Player to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An instance of <see cref="PlayerGamesResponseContract"/>.</returns>
        Task<PlayerGamesResponseContract?> GetPlayersGames(Guid playerId, CancellationToken cancellationToken);
    }
}
