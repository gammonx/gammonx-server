namespace GammonX.DynamoDb.Repository
{
    /// <summary>
    /// Provides DynamoDB batch-write operations used by bulk maintenance workflows.
    /// </summary>
    public interface IDynamoDbBatchWriter
    {
        /// <summary>
        /// Deletes the specified items in DynamoDB batch-write chunks.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="keys">Primary and secondary keys of the items to delete.</param>
        /// <returns>A task to be awaited.</returns>
        Task BatchDeleteAsync<T>(IEnumerable<(Guid PkId, string Sk)> keys);
    }
}