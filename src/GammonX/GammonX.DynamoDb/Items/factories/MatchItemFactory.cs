using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	// <inheritdoc />
	public class MatchItemFactory : IItemFactory<MatchItem>
	{
		// <inheritdoc />
		public string PKFormat => "MATCH#{0}";

		// <inheritdoc />
		public string SKFormat => "DETAILS#{0}";

		// <inheritdoc />
		public string SKPrefix => "DETAILS#";

		/// <summary>
		/// Format for GSI1SK like 'MATCH#888#Backgammon#7PointGame#Ranked#{WON|LOST|NOTFINISHED}'
		/// </summary>
		public string GSI1PKFormat => "PLAYER#{0}";

		// <inheritdoc />
		public string GSI1SKFormat => "MATCH#{0}#{1}#{2}#{3}#{4}";

		// <inheritdoc />
		public string GSI1SKPrefix => "MATCH#";

		// <inheritdoc />
		public MatchItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			var matchItem = new MatchItem
			{
				Id = Guid.Parse(item["Id"].S),
				PlayerId = Guid.Parse(item["PlayerId"].S),
				Points = int.Parse(item["Points"].N),
				Length = int.Parse(item["Length"].N),
				Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
				Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
				Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
				StartedAt = DateTime.Parse(item["StartedAt"].S),
				EndedAt = DateTime.Parse(item["EndedAt"].S),
				Duration = TimeSpan.Parse(item["Duration"].S),
				AvgDuration = TimeSpan.Parse(item["AvgDuration"].S),
				Result = Enum.Parse<MatchResult>(item["Result"].N, true),
				AvgDoubleDices = int.Parse(item["AvgDoubleDices"].N),
				AvgPipesLeft = int.Parse(item["AvgPipesLeft"].N),
				AvgTurns = int.Parse(item["AvgTurns"].N),
				BackGammons = int.Parse(item["BackGammons"].N),
				Gammons = int.Parse(item["Gammons"].N),
				AvgDoubles = double.Parse(item["AvgDoubles"].N)
			};
			return matchItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(MatchItem item)
		{
			var variantStr = item.Variant.ToString();
			var modusStr = item.Modus.ToString();
			var typeStr = item.Type.ToString();
			var resultStr = item.Result.ToString();
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "GSI1PK", new AttributeValue(item.GSI1PK) },
				{ "GSI1SK", new AttributeValue(item.GSI1SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "Id", new AttributeValue(item.Id.ToString()) },
				{ "PlayerId", new AttributeValue(item.PlayerId.ToString()) },
				{ "Points", new AttributeValue() { N = item.Points.ToString() } },
				{ "Length", new AttributeValue() { N = item.Length.ToString() } },
				{ "Variant", new AttributeValue(variantStr) },
				{ "Modus", new AttributeValue(modusStr) },
				{ "Type", new AttributeValue(typeStr) },
				{ "StartedAt", new AttributeValue { S = item.StartedAt.ToString("o") } },
				{ "EndedAt", new AttributeValue { S = item.EndedAt.ToString("o") } },
				{ "AvgPipesLeft", new AttributeValue { N = item.AvgPipesLeft.ToString() } },
				{ "AvgDoubleDices", new AttributeValue { N = item.AvgDoubleDices.ToString() } },
				{ "Gammons", new AttributeValue { N = item.Gammons.ToString() } },
				{ "BackGammons", new AttributeValue { N = item.BackGammons.ToString() } },
				{ "AvgTurns", new AttributeValue { N = item.AvgTurns.ToString() } },
				{ "AvgDoubles", new AttributeValue { N = item.AvgDoubles.ToString() } },
				{ "Duration", new AttributeValue { S = item.Duration.ToString() } },
				{ "AvgDuration", new AttributeValue { S = item.AvgDuration.ToString() } },
				{ "Result", new AttributeValue(resultStr) },
			};
			return itemDict;
		}
	}
}
