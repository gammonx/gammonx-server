using Amazon.DynamoDBv2.DataModel;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Items
{
	public class MatchItem
	{
		/// <summary>
		/// Gets a primary key like 'MATCH#{matchId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'DETAILS#{WON|LOST}'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		/// <summary>
		/// Gets the global search index primary key. (e.g. "PLAYER#{guid}")
		/// </summary>
		[DynamoDBGlobalSecondaryIndexHashKey("GSI1PK")]
		public string GSI1PK => ConstructGS1PK();

		/// <summary>
		/// Gets the global search index sort key. (e.g. "MATCH#888#Backgammon#7PointGame#Ranked#{WON|LOST|NOTFINISHED}")
		/// </summary>
		[DynamoDBGlobalSecondaryIndexRangeKey("GSI1SK")]
		public string GSI1SK => ConstructGS1SK();

		/// <summary>
		/// Gets or sets the match id.
		/// </summary>
		public Guid Id { get; set; } = Guid.Empty;

		/// <summary>
		/// Gets or sets the id of the <see cref="MatchItem"/> this rating belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

		public int Points { get; set; } = 0;

		/// <summary>
		/// Gets or sets the amount of games played in the related match.
		/// </summary>
		public int Length { get; set; } = 0;

		public string ItemType { get; } = ItemTypes.MatchItemType;

		public MatchVariant Variant { get; set; } = MatchVariant.Unknown;

		public MatchModus Modus { get; set; } = MatchModus.Unknown;

		public MatchType Type { get; set; } = MatchType.Unknown;

		public DateTime StartedAt { get; set; } = DateTime.UtcNow;

		public DateTime EndedAt { get; set; } = DateTime.UtcNow;

		public MatchResult Result { get; set; } = MatchResult.Unknown;

		/// <summary>
		/// Gets or sets the average pipes left for the games which were lost.
		/// </summary>
		public double AvgPipesLeft { get; set; } = 0;

		/// <summary>
		/// Gets or sets the average amount of double dices rolled per game
		/// </summary>
		public double AvgDoubleDices { get; set; } = 0;

		/// <summary>
		/// Gets or sets the amount of <see cref="GameResult.Gammon"/> wins.
		/// </summary>
		public int Gammons { get; set; } = 0;

		/// <summary>
		/// Gets or sets the amount of <see cref="GameResult.Backgammon"/> wins.
		/// </summary>
		public int Backgammons { get; set; } = 0;

		/// <summary>
		/// Gets or sets the average turns per game.
		/// </summary>
		public int AvgTurns { get; set; } = 0;

		/// <summary>
		/// Gets or sets the average game duration.
		/// </summary>
		public TimeSpan AvgDuration { get; set; } = TimeSpan.Zero;

		/// <summary>
		/// Gets the duration of the match.
		/// </summary>
		public TimeSpan Duration { get; set; } = TimeSpan.Zero;

		/// <summary>
		/// Gets the average amount of doubling cube offers.
		/// </summary>
		public double AvgDoubles { get; set; } = 0;

		private string ConstructPK()
		{
			var factory = new MatchItemFactory();
			return string.Format(factory.PKFormat, Id);
		}

		private string ConstructSK()
		{
			var factory = new MatchItemFactory();
			var wonOrLost = WonOrLost(Result);
			return string.Format(factory.SKFormat, wonOrLost);
		}

		private string ConstructGS1PK()
		{
			var factory = new MatchItemFactory();
			return string.Format(factory.GSI1PKFormat, PlayerId);
		}

		private string ConstructGS1SK()
		{
			var factory = new MatchItemFactory();
			var variantStr = Variant.ToString();
			var modusStr = Modus.ToString();
			var typeStr = Type.ToString();
			var wonOrLost = WonOrLost(Result);
			return string.Format(factory.GSI1SKFormat, variantStr, typeStr, modusStr, wonOrLost);
		}

		private static string WonOrLost(MatchResult result)
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
