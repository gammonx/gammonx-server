namespace GammonX.DynamoDb.Repository
{
    /// <summary>
    /// Provides strongly consistent reads for items identified by partition and sort key.
    /// </summary>
    public interface IDynamoDbConsistentReader
    {
        /// <summary>
        /// Retrieves items of type <typeparamref name="T"/> identified by the specified partition key and sort key, using a strongly consistent read.
        /// </summary>
        /// <typeparam name="T">The type of the items to retrieve.</typeparam>
        /// <param name="pkId">The partition key of the items to retrieve.</param>
        /// <param name="sk">The sort key of the items to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved items of type <typeparamref name="T"/>.</returns>
        Task<IEnumerable<T>> GetItemsConsistentlyAsync<T>(Guid pkId, string sk);
    }
}