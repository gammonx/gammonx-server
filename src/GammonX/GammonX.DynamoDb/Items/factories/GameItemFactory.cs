using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;

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
		/// Format for GSI1SK like 'GAME#{gameModus}#{WINNER|LOSER}'
		/// </summary>
		public string GSI1SKFormat => "GAME#{0}#{1}";

		// <inheritdoc />
		public string GSI1SKPrefix => "GAME#";

		// <inheritdoc />
		public GameItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			var value = item["DoublingCubeValue"].N;
			var gameItem = new GameItem
			{
				Id = Guid.Parse(item["Id"].S),
				PlayerId = Guid.Parse(item["PlayerId"].S),
                MatchId = Guid.Parse(item["MatchId"].S),
                Points = int.Parse(item["Points"].N),
				Length = int.Parse(item["Length"].N),
				Modus = Enum.Parse<GameModus>(item["Modus"].S, true),
				StartedAt = DateTime.Parse(item["StartedAt"].S),
				EndedAt = DateTime.Parse(item["EndedAt"].S),
				Result = Enum.Parse<GameResult>(item["Result"].S, true),
				DiceDoubles = int.Parse(item["DiceDoubles"].N),
				DoublingCubeValue = value != null ? int.Parse(value) : null,
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
				{ "DoublingCubeValue", new AttributeValue { S = item.DoublingCubeValue.ToString() } },
				{ "Duration", new AttributeValue { S = item.Duration.ToString() } },
				{ "PipesLeft", new AttributeValue { N = item.PipesLeft.ToString() } }
			};
			return itemDict;
		}
	}
}
