using Amazon.DynamoDBv2.Model;

using System.Globalization;

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
		/// Format for GSI1SK like 'MATCH#{Variant}#{Type}#{Modus}#{WON|LOST|NOTFINISHED#{PlayerId}'.
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
			var botLevel = BotLevel.Unknown;
			if (item.TryGetValue("BotLevel", out var botLevelAttribute) && botLevelAttribute.S is { Length: > 0 } botLevelValue)
				botLevel = Enum.Parse<BotLevel>(botLevelValue, true);

			var matchItem = new MatchItem
			{
				Id = Guid.Parse(item["Id"].S),
				PlayerId = Guid.Parse(item["PlayerId"].S),
				Points = int.Parse(item["Points"].N, CultureInfo.InvariantCulture),
				Length = int.Parse(item["Length"].N, CultureInfo.InvariantCulture),
				Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
				Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
				Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
				BotLevel = botLevel,
				StartedAt = DateTimeHelper.ParseUtc(item["StartedAt"].S),
				EndedAt = DateTimeHelper.ParseUtc(item["EndedAt"].S),
				Duration = DateTimeHelper.ParseDurationTicks(item["Duration"].N),
				AvgDuration = DateTimeHelper.ParseDurationTicks(item["AvgDuration"].N),
				Result = Enum.Parse<MatchResult>(item["Result"].S, true),
				AvgDoubleDices = double.Parse(item["AvgDoubleDices"].N, CultureInfo.InvariantCulture),
				AvgPipesLeft = double.Parse(item["AvgPipesLeft"].N, CultureInfo.InvariantCulture),
				AvgTurns = int.Parse(item["AvgTurns"].N, CultureInfo.InvariantCulture),
				Backgammons = int.Parse(item["BackGammons"].N, CultureInfo.InvariantCulture),
				Gammons = int.Parse(item["Gammons"].N, CultureInfo.InvariantCulture),
				AvgDoubles = double.Parse(item["AvgDoubles"].N, CultureInfo.InvariantCulture)
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
				{ "Points", new AttributeValue() { N = item.Points.ToString(CultureInfo.InvariantCulture) } },
				{ "Length", new AttributeValue() { N = item.Length.ToString(CultureInfo.InvariantCulture) } },
				{ "Variant", new AttributeValue(variantStr) },
				{ "Modus", new AttributeValue(modusStr) },
				{ "Type", new AttributeValue(typeStr) },
				{ "BotLevel", new AttributeValue(item.BotLevel.ToString()) },
				{ "StartedAt", new AttributeValue { S = DateTimeHelper.FormatUtc(item.StartedAt) } },
				{ "EndedAt", new AttributeValue { S = DateTimeHelper.FormatUtc(item.EndedAt) } },
				{ "AvgPipesLeft", new AttributeValue { N = item.AvgPipesLeft.ToString(CultureInfo.InvariantCulture) } },
				{ "AvgDoubleDices", new AttributeValue { N = item.AvgDoubleDices.ToString(CultureInfo.InvariantCulture) } },
				{ "Gammons", new AttributeValue { N = item.Gammons.ToString(CultureInfo.InvariantCulture) } },
				{ "BackGammons", new AttributeValue { N = item.Backgammons.ToString(CultureInfo.InvariantCulture) } },
				{ "AvgTurns", new AttributeValue { N = item.AvgTurns.ToString(CultureInfo.InvariantCulture) } },
				{ "AvgDoubles", new AttributeValue { N = item.AvgDoubles.ToString(CultureInfo.InvariantCulture) } },
				{ "Duration", new AttributeValue { N = DateTimeHelper.FormatDurationTicks(item.Duration) } },
				{ "AvgDuration", new AttributeValue { N = DateTimeHelper.FormatDurationTicks(item.AvgDuration) } },
				{ "Result", new AttributeValue(resultStr) },
			};
			return itemDict;
		}
	}
}
