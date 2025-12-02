using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	public class MatchHistoryItemFactory : IItemFactory<MatchHistoryItem>
	{
		// <inheritdoc />
		public string PKFormat => "MATCH#{0}";

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
		public MatchHistoryItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			var matchHistoryItem = new MatchHistoryItem
			{
				MatchId = Guid.Parse(item["MatchId"].S),
				Data = item["Data"].S,
				Format = Enum.Parse<HistoryFormat>(item["Format"].S)
			};
			return matchHistoryItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(MatchHistoryItem item)
		{
			var formatString = item.Format.ToString();
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "MatchId", new AttributeValue(item.MatchId.ToString()) },
				{ "Format", new AttributeValue(formatString) },
				{ "Data", new AttributeValue(item.Data) },
			};
			return itemDict;
		}
	}
}
