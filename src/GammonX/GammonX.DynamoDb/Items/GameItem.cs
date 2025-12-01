using Amazon.DynamoDBv2.DataModel;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	public class GameItem
	{
		/// <summary>
		/// Gets a primary key like 'MATCH#{matchId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'GAME#{gameId}#{WON|LOST}'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		/// <summary>
		/// Gets the global search index primary key. (e.g. "PLAYER#{guid}")
		/// </summary>
		[DynamoDBGlobalSecondaryIndexHashKey("GSI1PK")]
		public string GSI1PK => ConstructGS1PK();

		/// <summary>
		/// Gets the global search index sort key. (e.g. "GAME#Portes#{WON|LOST|NOTFINISHED}")
		/// </summary>
		[DynamoDBGlobalSecondaryIndexRangeKey("GSI1SK")]
		public string GSI1SK => ConstructGS1SK();

		/// <summary>
		/// Gets or sets the game id.
		/// </summary>
		public Guid Id { get; set; } = Guid.Empty;

		/// <summary>
		/// Gets or sets the id of the <see cref="GameHistoryItem"/> this rating belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

		/// <summary>
		/// Gets or sets the points awarded to the given palyer.
		/// If the game was lost the amount of points must be <c>0</c>.
		/// </summary>
		public int Points { get; set; } = 0;

		/// <summary>
		/// Gets or sets the amount of turns played in the related game by the given player.
		/// </summary>
		public int Length { get; set; } = 0;

		public string ItemType { get; } = ItemTypes.GameItemType;

		public GameModus Modus { get; set; } = GameModus.Unknown;

		public DateTime StartedAt { get; set; } = DateTime.UtcNow;

		public DateTime EndedAt { get; set; } = DateTime.UtcNow;

		public TimeSpan Duration { get; set; } = TimeSpan.Zero;

		/// <summary>
		/// Amount of pipes left for the related player.
		/// If the game is won, the amount must be <c>0</c>.
		/// </summary>
		public int PipesLeft { get; set; } = 0;

		public int DiceDoubles { get; set; } = 0;

		public GameResult Result { get; set; } = GameResult.Unknown;

		/// <summary>
		/// Gets or sets the doubling cube value.
		/// Returns <c>null</c> if not applicable for the game modus.
		/// </summary>
		public int? DoublingCubeValue { get; set; } = 0;

		private string ConstructPK()
		{
			var factory = ItemFactoryCreator.Create<GameItem>();
            return string.Format(factory.PKFormat, Id);
		}

		private string ConstructSK()
		{
			var factory = ItemFactoryCreator.Create<GameItem>();
            var wonOrLost = WonOrLost(Result);
			return string.Format(factory.SKFormat, Id, wonOrLost);
		}

		private string ConstructGS1PK()
		{
			var factory = ItemFactoryCreator.Create<GameItem>();
            return string.Format(factory.GSI1PKFormat, PlayerId);
		}

		private string ConstructGS1SK()
		{
			var factory = ItemFactoryCreator.Create<GameItem>();
            var modusStr = Modus.ToString();
			var wonOrLost = WonOrLost(Result);
			return string.Format(factory.GSI1SKFormat, modusStr, wonOrLost);
		}

		private static string WonOrLost(GameResult result)
		{
			var value = result.HasWon();
			if (value.HasValue)
			{
				return value.Value ? "WON" : "LOST";
			}
			return "NOTFINISHED";
		}
	}
}
