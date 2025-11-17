using Amazon.DynamoDBv2.DataModel;

using GammonX.Server.Models;

namespace GammonX.Server.Data.Entities
{
	public class MatchItem
	{
		public const string PKFormat = "MATCH#{0}";
		public const string SKFormat = "DETAILS#{0}";
		public const string SKPrefix = "DETAILS#";
		public const string GSI1PKFormat = "PLAYER#{0}";
		/// <summary>
		/// Format for GSI1SK like 'MATCH#888#Backgammon#7PointGame#Ranked#{WINNER|LOSER}'
		/// </summary>
		public const string GSI1SKFormat = "MATCH#{0}#{1}#{2}#{3}#{4}";
		public const string GSI1SKPrefix = "MATCH#";

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
		/// Gets the global search index sort key. (e.g. "MATCH#888#Backgammon#7PointGame#Ranked#{WON|LOST}")
		/// </summary>
		[DynamoDBGlobalSecondaryIndexRangeKey("GSI1SK")]
		public string GSI1SK => ConstructGS1SK();

		public Guid Id { get; set; } = Guid.Empty;

		/// <summary>
		/// Gets or sets the id of the <see cref="PlayerItem"/> this rating belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

		public int Points { get; set; } = 0;

		public int Length { get; set; } = 0;

		public string ItemType { get; } = ItemTypes.MatchItemType;

		public WellKnownMatchVariant Variant { get; set; } = WellKnownMatchVariant.Unknown;

		public WellKnownMatchModus Modus { get; set; } = WellKnownMatchModus.Unknown;

		public WellKnownMatchType Type { get; set; } = WellKnownMatchType.Unknown;

		public DateTime StartedAt { get; set; } = DateTime.UtcNow;

		public DateTime EndedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		/// Gets or sets a boolean if the given match item relates to the winner or loser of the match.
		/// </summary>
		public bool Won { get; set; } = false;

		private string ConstructPK()
		{
			return string.Format(PKFormat, Id);
		}

		private string ConstructSK()
		{
			var wonOrLost = Won ? "WON" : "LOST";
			return string.Format(SKFormat, wonOrLost);
		}

		private string ConstructGS1PK()
		{
			return string.Format(GSI1PKFormat, PlayerId);
		}

		private string ConstructGS1SK()
		{
			var variantStr = Variant.ToString();
			var modusStr = Modus.ToString();
			var typeStr = Type.ToString();
			var wonOrLost = Won ? "WON" : "LOST";
			return string.Format(GSI1SKFormat, Id, variantStr, typeStr, modusStr, wonOrLost);
		}
	}
}
