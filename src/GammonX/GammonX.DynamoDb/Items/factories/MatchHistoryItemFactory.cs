using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;
using GammonX.Models.History;
using GammonX.Models.History.MAT;

namespace GammonX.DynamoDb.Items
{
	public class MatchHistoryItemFactory : IItemFactory<MatchHistoryItem>
	{
		// <inheritdoc />
		public string PKFormat => "MATCH#{0:D}";

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
			var format = Enum.Parse<HistoryFormat>(item["Format"].S);
            MATParser parser = HistoryParserFactory.Create<MATParser>(format);
			var matchHistoryItem = new MatchHistoryItem
			{
				MatchId = Guid.Parse(item["MatchId"].S),
				Data = parser.DecodeBinary(item["Data"].B?.ToArray() ?? throw new FormatException("Match history Data must be stored as binary content.")),
				Format = Enum.Parse<HistoryFormat>(item["Format"].S)
			};
			return matchHistoryItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(MatchHistoryItem item)
		{
			var formatString = item.Format.ToString();
            MATParser parser = HistoryParserFactory.Create<MATParser>(item.Format);
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "MatchId", new AttributeValue(item.MatchId.ToString("D")) },
				{ "Format", new AttributeValue(formatString) },
				{ "Data", new AttributeValue { B = new MemoryStream(parser.EncodeBinary(item.Data), writable: false) } },
			};
			return itemDict;
		}
	}
}
