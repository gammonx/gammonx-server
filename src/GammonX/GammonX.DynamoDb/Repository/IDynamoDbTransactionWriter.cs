namespace GammonX.DynamoDb.Repository
{
    /// <summary>
    /// Provides atomic DynamoDB write transactions.
    /// </summary>
    public interface IDynamoDbTransactionWriter
    {
        /// <summary>
        /// Atomically persists all specified put operations.
        /// </summary>
        /// <param name="operations">Typed put operations to persist.</param>
        /// <returns>A task to be awaited.</returns>
        Task TransactPutAsync(IEnumerable<DynamoDbPutOperation> operations);
    }
}