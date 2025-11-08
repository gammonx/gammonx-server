using GammonX.Server.Data.Entities;
using GammonX.Server.Models;

namespace GammonX.Server.Data.Repository
{
	/// <summary>
	/// Provides capabilities to interact with the player item type.
	/// </summary>
	public interface IPlayerRepository
	{
		/// <summary>
		/// Gets the player by its unique identifier.
		/// </summary>
		/// <param name="playerId">Unique player identifier.</param>
		/// <returns>Player item type instance.</returns>
		Task<PlayerItem?> GetAsync(Guid playerId);

		/// <summary>
		/// Gets the player rating by its unique identifier.
		/// </summary>
		/// <remarks>
		/// Type is currently not important as only one type is supported in ranked.
		/// </remarks>
		/// <param name="playerId">Unique player identifier.</param>
		/// <param name="variant">Match variant.</param>
		/// <param name="modus">Match modus. Normally just ranked.</param>
		/// <returns>Player rating for a given variant and modus.</returns>
		Task<IEnumerable<PlayerRatingItem>> GetRatingsAsync(Guid playerId, WellKnownMatchVariant variant, WellKnownMatchModus modus);

		/// <summary>
		/// Saves the given player item type.
		/// </summary>
		/// <param name="player">Player item to save.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync(PlayerItem player);
	}
}
