using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Repository
{
	/// <summary>
	/// Provides capabilities to interact with the GammonX dynamo db table.
	/// </summary>
	public interface IDynamoDbRepository
	{
		#region Generic ItemType

		/// <summary>
		/// Gets items of <typeparamref name="T"/> by its PK and SKPrefix.
		/// </summary>
		/// <typeparam name="T">Item Type.</typeparam>
		/// <param name="pkId">Primary key guid.</param>
		/// <returns>A list of items matching the given <paramref name="pkId"/> and sk prefix.</returns>
		Task<IEnumerable<T>> GetItems<T>(Guid pkId);

		/// <summary>
		/// Gets items of <typeparamref name="T"/> by its GSI1PK and GSI1SKPrefix.
		/// </summary>
		/// <typeparam name="T">Item Type.</typeparam>
		/// <param name="gsi1PkId">Primary key guid.</param>
		/// <returns>A list of items matching the given <paramref name="gsi1PkId"/> and sk prefix.</returns>
		Task<IEnumerable<T>> GetItemsByGSIPK<T>(Guid gsi1PkId);

		/// <summary>
		/// Gets items of <typeparamref name="T"/> by its GSI1PK and starting with <paramref name="gsi1SK"/>.
		/// </summary>
		/// <typeparam name="T">Item Type.</typeparam>
		/// <param name="gsi1PkId">Primary key guid.</param>
		/// <param name="gsi1SK">Sort key to search for.</param>
		/// <returns>A list of items matching the given <paramref name="gsi1PkId"/> and <paramref name="gsi1SK"/>.</returns>
		Task<IEnumerable<T>> GetItemsByGSIPK<T>(Guid gsi1PkId, string gsi1SK);

		/// <summary>
		/// Saves the item of given <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Item type.</typeparam>
		/// <param name="item">Item to persist.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync<T>(T item);

		#endregion Generic ItemType

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
