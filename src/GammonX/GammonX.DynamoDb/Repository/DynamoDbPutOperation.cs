using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Repository
{
    /// <summary>
    /// Describes a typed item put within a DynamoDB transaction.
    /// </summary>
    public sealed class DynamoDbPutOperation
    {
        internal Dictionary<string, AttributeValue> Item { get; }

        internal string? ConditionExpression { get; }

        internal IReadOnlyDictionary<string, string> ExpressionAttributeNames { get; }

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
    }
}