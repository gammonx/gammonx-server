using Amazon.DynamoDBv2.Model;

using System.Globalization;

using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Repository
{
    /// <summary>
    /// Describes a typed item put within a DynamoDB transaction.
    /// </summary>
    public sealed class DynamoDbPutOperation
    {
        /// <summary>
        /// Gets the item to be put in the DynamoDB transaction.
        /// </summary>
        internal Dictionary<string, AttributeValue> Item { get; }

        /// <summary>
        /// Gets the condition expression for the put operation, if any.
        /// </summary>
        internal string? ConditionExpression { get; }

        /// <summary>
        /// Gets the expression attribute names for the put operation, if any.
        /// </summary>
        internal IReadOnlyDictionary<string, string> ExpressionAttributeNames { get; }

        /// <summary>
        /// Gets the expression attribute values for the put operation, if any.
        /// </summary>
        internal IReadOnlyDictionary<string, AttributeValue> ExpressionAttributeValues { get; }

        private DynamoDbPutOperation(
            Dictionary<string, AttributeValue> item,
            string? conditionExpression,
            IReadOnlyDictionary<string, string>? expressionAttributeNames,
            IReadOnlyDictionary<string, AttributeValue>? expressionAttributeValues)
        {
            Item = item;
            ConditionExpression = conditionExpression;
            ExpressionAttributeNames = expressionAttributeNames ?? new Dictionary<string, string>();
            ExpressionAttributeValues = expressionAttributeValues ?? new Dictionary<string, AttributeValue>();
        }

        /// <summary>
        /// Creates a new DynamoDbPutOperation for the specified item.
        /// </summary>
        /// <typeparam name="T">The type of the item to put.</typeparam>
        /// <param name="item">The item to put in the DynamoDB transaction.</param>
        /// <param name="conditionExpression">The condition expression for the put operation, if any.</param>
        /// <param name="expressionAttributeNames">The expression attribute names for the put operation, if any.</param>
        /// <param name="expressionAttributeValues">The expression attribute values for the put operation, if any.</param>
        /// <returns>A new instance of <see cref="DynamoDbPutOperation"/>.</returns>
        public static DynamoDbPutOperation Create<T>(
            T item,
            string? conditionExpression = null,
            IReadOnlyDictionary<string, string>? expressionAttributeNames = null,
            IReadOnlyDictionary<string, AttributeValue>? expressionAttributeValues = null)
        {
            ArgumentNullException.ThrowIfNull(item);

            var factory = ItemFactoryCreator.Create<T>();

            return new DynamoDbPutOperation(
                factory.CreateItem(item),
                conditionExpression,
                expressionAttributeNames,
                expressionAttributeValues);
        }

        /// <summary>
        /// Creates a new DynamoDbPutOperation for the specified item with an expected revision.
        /// </summary>
        /// <param name="item">The item to put in the DynamoDB transaction.</param>
        /// <param name="expectedRevision">The expected revision of the item.</param>
        /// <typeparam name="T">The type of the item to put.</typeparam>
        /// <returns>A new instance of <see cref="DynamoDbPutOperation"/> configured with the expected revision.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DynamoDbPutOperation CreateVersioned<T>(T item, int expectedRevision)
        {
            if (expectedRevision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedRevision));
            }

            return Create(
                item,
                "attribute_not_exists(#revision) OR #revision = :expectedRevision",
                new Dictionary<string, string> { { "#revision", "Revision" } },
                new Dictionary<string, AttributeValue>
                {
                    { ":expectedRevision", new AttributeValue { N = expectedRevision.ToString(CultureInfo.InvariantCulture) } }
                });
        }
    }
}