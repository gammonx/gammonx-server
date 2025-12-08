using Amazon.DynamoDBv2.Model;
using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	// <inheritdoc />
	public class GameHistoryItemFactory : IItemFactory<GameHistoryItem>
	{
		// <inheritdoc />
		public string PKFormat => "GAME#{0}";

		// <inheritdoc />
		public string SKFormat => "HISTORY";

		// <inheritdoc />
		public string SKPrefix => "HISTORY";

		// <inheritdoc />
		public string GSI1PKFormat => throw new InvalidOperationException("No GSI for game history item type yet.");

		// <inheritdoc />
		public string GSI1SKFormat => throw new InvalidOperationException("No GSI for game history item type yet.");

		// <inheritdoc />
		public string GSI1SKPrefix => throw new InvalidOperationException("No GSI for game history item type yet.");

		// <inheritdoc />
		public GameHistoryItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			var gameHistoryItem = new GameHistoryItem
			{
				GameId = Guid.Parse(item["GameId"].S),
				Data = item["Data"].S,
				Format = Enum.Parse<HistoryFormat>(item["Format"].S)
			};
			return gameHistoryItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(GameHistoryItem item)
		{
			var formatString = item.Format.ToString();
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "GameId", new AttributeValue(item.GameId.ToString()) },
				{ "Format", new AttributeValue(formatString) },
				{ "Data", new AttributeValue(item.Data) },
			};
			return itemDict;
		}
	}
}
