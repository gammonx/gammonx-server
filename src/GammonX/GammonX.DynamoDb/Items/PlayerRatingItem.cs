using Amazon.DynamoDBv2.DataModel;
using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	public class PlayerRatingItem
	{
		public const string PKFormat = "PLAYER#{0}";

		public const string SKFormat = "RATING#{0}";

		public const string SKPrefix = "RATING#";

		/// <summary>
		/// Gets a primary key like 'PLAYER#{playerId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'RATING#{Variant}'. Ratings only exists for ranked (modus) and a single type.
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		/// <summary>
		/// Gets or sets the id of the <see cref="PlayerItem"/> this rating belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

		public string ItemType { get; } = ItemTypes.PlayerRatingItemType;

		public MatchVariant Variant { get; set; } = MatchVariant.Unknown;

		public MatchModus Modus { get; set; } = MatchModus.Unknown;

		public Models.Enums.MatchType Type { get; set; } = Models.Enums.MatchType.Unknown;

		public int Rating { get; set; } = 1200;

		public int HighestRating { get; set; } = 1200;

		public int LowestRating { get; set; } = 1200;

		private string ConstructPK()
		{
			return string.Format(PKFormat, PlayerId);
		}

		private string ConstructSK()
		{
			return string.Format(SKFormat, Variant);
		}
	}
}
