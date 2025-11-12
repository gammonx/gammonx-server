using GammonX.Server.Data.Entities;

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

		/// <summary>
		/// Gets the match by the given <paramref name="matchId"/>.
		/// </summary>
		/// <remarks>
		/// Will always return 2 match items. One for the winner and one for the loser.
		/// </remarks>
		/// <param name="matchId">Id of the match.</param>
		/// <returns>A list of match items.</returns>
		Task<IEnumerable<MatchItem>> GetMatchesAsync(Guid matchId);

		/// <summary>
		/// Gets all matches played by the given <paramref name="playerId"/>.
		/// </summary>
		/// <remarks>
		/// The list will includes matches of all variants, types and modus.
		/// </remarks>
		/// <param name="playerId">Id of the player.</param>
		/// <returns>A list of match items.</returns>
		Task<IEnumerable<MatchItem>> GetMatchesOfPlayerAsync(Guid playerId);

		/// <summary>
		/// Saves the given <paramref name="match"/>.
		/// </summary>
		/// <param name="match">Match to save.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync(MatchItem match);
	}
}
