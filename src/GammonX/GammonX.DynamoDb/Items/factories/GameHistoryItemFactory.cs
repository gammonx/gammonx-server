using Amazon.DynamoDBv2.Model;
using GammonX.Models.Enums;
using GammonX.Models.History;
using GammonX.Models.History.MAT;

namespace GammonX.DynamoDb.Items
{
	// <inheritdoc />
	public class GameHistoryItemFactory : IItemFactory<GameHistoryItem>
	{
		// <inheritdoc />
		public string PKFormat => "GAME#{0:D}";

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
            var format = Enum.Parse<HistoryFormat>(item["Format"].S);
            MATParser parser = HistoryParserFactory.Create<MATParser>(format);
			var gameHistoryItem = new GameHistoryItem
			{
				GameId = Guid.Parse(item["GameId"].S),
				Data = parser.DecodeBinary(item["Data"].B?.ToArray() ?? throw new FormatException("Game history Data must be stored as binary content.")),
				Format = Enum.Parse<HistoryFormat>(item["Format"].S)
			};
			return gameHistoryItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(GameHistoryItem item)
		{
			var formatString = item.Format.ToString();
            MATParser parser = HistoryParserFactory.Create<MATParser>(item.Format);
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "GameId", new AttributeValue(item.GameId.ToString("D")) },
				{ "Format", new AttributeValue(formatString) },
				{ "Data", new AttributeValue { B = new MemoryStream(parser.EncodeBinary(item.Data), writable: false) } },
			};
			return itemDict;
		}
	}
}
