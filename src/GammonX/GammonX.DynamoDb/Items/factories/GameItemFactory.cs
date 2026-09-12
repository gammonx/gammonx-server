using Amazon.DynamoDBv2.Model;

using System.Globalization;

using GammonX.Models.Enums;
using GammonX.Models.Helpers;

namespace GammonX.DynamoDb.Items
{
	// <inheritdoc />
	public class GameItemFactory : IItemFactory<GameItem>
	{
		// <inheritdoc />
		public string PKFormat => "MATCH#{0}";

		// <inheritdoc />
		public string SKPrefix => "GAME#";

		// <inheritdoc />
		public string SKFormat => "GAME#{0}#{1}";

		// <inheritdoc />
		public string GSI1PKFormat => "PLAYER#{0}";

		/// <summary>
		/// Format for GSI1SK like 'GAME#{GameModus}#{WON|LOST|NOTFINISHED#{PlayerId}'.
		/// </summary>
		public string GSI1SKFormat => "GAME#{0}#{1}";

		// <inheritdoc />
		public string GSI1SKPrefix => "GAME#";

		// <inheritdoc />
		public GameItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			int? doublingCubeValue = null;
			if (item.TryGetValue("DoublingCubeValue", out var cubeAttribute) && cubeAttribute.NULL != true)
			{
				var value = string.IsNullOrWhiteSpace(cubeAttribute.N) ? cubeAttribute.S : cubeAttribute.N;
				if (!string.IsNullOrWhiteSpace(value))
					doublingCubeValue = int.Parse(value, CultureInfo.InvariantCulture);
			}

			var gameItem = new GameItem
			{
				Id = Guid.Parse(item["Id"].S),
				PlayerId = Guid.Parse(item["PlayerId"].S),
                MatchId = Guid.Parse(item["MatchId"].S),
                Points = int.Parse(item["Points"].N),
				Length = int.Parse(item["Length"].N),
				Modus = Enum.Parse<GameModus>(item["Modus"].S, true),
				StartedAt = DateTimeHelper.ParseFlexible(item["StartedAt"].S),
				EndedAt = DateTimeHelper.ParseFlexible(item["EndedAt"].S),
				Result = Enum.Parse<GameResult>(item["Result"].S, true),
				DiceDoubles = int.Parse(item["DiceDoubles"].N),
				DoublingCubeValue = doublingCubeValue,
				Duration = TimeSpan.Parse(item["Duration"].S),
				PipesLeft = int.Parse(item["PipesLeft"].N)
			};
			return gameItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(GameItem item)
		{
			var modusStr = item.Modus.ToString();
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
                { "MatchId", new AttributeValue(item.MatchId.ToString()) },
                { "Points", new AttributeValue() { N = item.Points.ToString() } },
				{ "Length", new AttributeValue() { N = item.Length.ToString() } },
				{ "Modus", new AttributeValue(modusStr) },
				{ "StartedAt", new AttributeValue { S = item.StartedAt.ToString("o") } },
				{ "EndedAt", new AttributeValue { S = item.EndedAt.ToString("o") } },
				{ "Result", new AttributeValue(resultStr) },
				{ "DiceDoubles", new AttributeValue { N = item.DiceDoubles.ToString() } },
				{ "DoublingCubeValue", item.DoublingCubeValue.HasValue
					? new AttributeValue { N = item.DoublingCubeValue.Value.ToString(CultureInfo.InvariantCulture) }
					: new AttributeValue { NULL = true } },
				{ "Duration", new AttributeValue { S = item.Duration.ToString() } },
				{ "PipesLeft", new AttributeValue { N = item.PipesLeft.ToString() } }
			};
			return itemDict;
		}
	}
}
