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
		Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId);

		/// <summary>
		/// Gets a list of <typeparamref name="T"/> by its PK and SK.
		/// </summary>
		/// <typeparam name="T">Item Type.</typeparam>
		/// <param name="pkId">Primary key guid</param>
		/// <param name="sk">Secondary key.</param>
		/// <returns>An lost of <typeparamref name="T"/> items. matching the given <paramref name="pkId"/> and <paramref name="sk"/>.</returns>
		Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId, string sk);

        /// <summary>
        /// Gets items of <typeparamref name="T"/> by its GSI1PK and GSI1SKPrefix.
        /// </summary>
        /// <typeparam name="T">Item Type.</typeparam>
        /// <param name="gsi1PkId">Primary key guid.</param>
        /// <returns>A list of items matching the given <paramref name="gsi1PkId"/> and sk prefix.</returns>
        Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId);

		/// <summary>
		/// Gets items of <typeparamref name="T"/> by its GSI1PK and starting with <paramref name="gsi1SK"/>.
		/// </summary>
		/// <typeparam name="T">Item Type.</typeparam>
		/// <param name="gsi1PkId">Primary key guid.</param>
		/// <param name="gsi1SK">Sort key to search for.</param>
		/// <returns>A list of items matching the given <paramref name="gsi1PkId"/> and <paramref name="gsi1SK"/>.</returns>
		Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId, string gsi1SK);

		/// <summary>
		/// Saves the item of given <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">Item type.</typeparam>
		/// <param name="item">Item to persist.</param>
		/// <returns>A task to be awaited.</returns>
		Task SaveAsync<T>(T item);

		/// <summary>
		/// Deletes the item of given <typeparamref name="T"/> with the specified <paramref name="pkId"/>.
		/// </summary>
		/// <typeparam name="T">Item type.</typeparam>
		/// <param name="pkId">Primary key guid.</param>
		/// <param name="sk">Secondary key string.</param>
		/// <returns>Returns a boolean indicating delete success.</returns>
		Task<bool> DeleteAsync<T>(Guid pkId, string sk);

        #endregion Generic ItemType
    }
}
