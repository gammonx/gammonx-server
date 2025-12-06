using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;
using GammonX.Models.Helpers;

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

		// <inheritdoc />
		public string GSI1PKFormat => "PLAYER#{0}";

		/// <summary>
		/// Format for GSI1SK like 'MATCH#{variant}#{type}#{modus}#{WON|LOST|NOTFINISHED}'
		/// </summary>
		public string GSI1SKFormat => "MATCH#{0}#{1}#{2}#{3}";

		/// <summary>
		/// Format for GSI1SK like 'MATCH#{variant}#{type}#{modus}'
		/// </summary>
		public string GSI1SKAllFormat => "MATCH#{0}#{1}#{2}";

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
				StartedAt = DateTimeHelper.ParseFlexible(item["StartedAt"].S),
				EndedAt = DateTimeHelper.ParseFlexible(item["EndedAt"].S),
				Duration = TimeSpan.Parse(item["Duration"].S),
				AvgDuration = TimeSpan.Parse(item["AvgDuration"].S),
				Result = Enum.Parse<MatchResult>(item["Result"].S, true),
				AvgDoubleDices = double.Parse(item["AvgDoubleDices"].N),
				AvgPipesLeft = double.Parse(item["AvgPipesLeft"].N),
				AvgTurns = int.Parse(item["AvgTurns"].N),
				Backgammons = int.Parse(item["BackGammons"].N),
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
				{ "StartedAt", new AttributeValue { S = item.StartedAt.ToString() } },
				{ "EndedAt", new AttributeValue { S = item.EndedAt.ToString() } },
				{ "AvgPipesLeft", new AttributeValue { N = item.AvgPipesLeft.ToString() } },
				{ "AvgDoubleDices", new AttributeValue { N = item.AvgDoubleDices.ToString() } },
				{ "Gammons", new AttributeValue { N = item.Gammons.ToString() } },
				{ "BackGammons", new AttributeValue { N = item.Backgammons.ToString() } },
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
