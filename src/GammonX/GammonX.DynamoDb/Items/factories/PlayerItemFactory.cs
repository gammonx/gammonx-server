using Amazon.DynamoDBv2.Model;

namespace GammonX.DynamoDb.Items
{
    // <inheritdoc />
    public class PlayerItemFactory : IItemFactory<PlayerItem>
    {
        // <inheritdoc />
        public string PKFormat => "PLAYER#{0}";

        // <inheritdoc />
        public string SKFormat => "PROFILE";

        // <inheritdoc />
        public string SKPrefix => "PROFILE";

        // <inheritdoc />
        public string GSI1PKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKPrefix => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public PlayerItem CreateItem(Dictionary<string, AttributeValue> item)
        {
            return new PlayerItem
            {
                Id = Guid.Parse(item["Id"].S),
                UserName = item["Username"].S,
                CreatedAt = DateTime.Parse(item["CreatedAt"].S)
            };
        }

        // <inheritdoc />
        public Dictionary<string, AttributeValue> CreateItem(PlayerItem item)
        {
            var itemDict = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue(item.PK) },
                { "SK", new AttributeValue(item.SK) },
                { "Id", new AttributeValue(item.Id.ToString()) },
                { "ItemType", new AttributeValue(item.ItemType) },
                { "Username", new AttributeValue(item.UserName) },
                { "CreatedAt", new AttributeValue { S = item.CreatedAt.ToString("o") } }
            };
            return itemDict;
        }
    }
}
