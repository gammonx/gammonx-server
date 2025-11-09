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
		/// Saves the given player item type.
		/// </summary>
		/// <param name="player">Player item to save.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync(PlayerItem player);

		/// <summary>
		/// Deletes the player with the given unique identifier.
		/// </summary>
		/// <param name="playerId">Unique player identifier.</param>
		/// <returns>Task to be awaited.</returns>
		Task DeleteAsync(Guid playerId);

		/// <summary>
		/// Gets the player ratings by its unique identifier.
		/// </summary>
		/// <remarks>
		/// Type is currently not important as only one type is supported in ranked.
		/// </remarks>
		/// <param name="playerId">Unique player identifier.</param>
		/// <returns>Player rating for a given variant and modus.</returns>
		Task<IEnumerable<PlayerRatingItem>> GetRatingsAsync(Guid playerId);

		/// <summary>
		/// Saves the given player rating item type.
		/// </summary>
		/// <param name="player">Player rating item to save.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync(PlayerRatingItem player);
	}
}
