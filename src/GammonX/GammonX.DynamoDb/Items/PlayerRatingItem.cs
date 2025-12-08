using Amazon.DynamoDBv2.DataModel;

using GammonX.DynamoDb.Stats;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	public class PlayerRatingItem
	{
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

        /// <summary>
        /// Gets or sets the ordinary rating of the player for the given variant/modus/type.
        /// </summary>
        public double Rating { get; set; } = Glicko2Constants.DefaultRating;

        /// <summary>
        /// Gets or sets the rating deviation of the player for the given variant/modus/type.
        /// </summary>
        /// <remarks>
        /// Asks on how uncertain is the rating?
        /// RD ranges:
		/// - Low RD (trusted rating): 30–80
		/// - Medium RD: 80–150
		/// - High RD(not enough recent games): 150–350
		/// RD increases automatically when inactive (because uncertainty grows).
		/// </remarks>
        public double RatingDeviation { get; set; } = Glicko2Constants.DefaultRD;

        /// <summary>
        /// Gets or sets the volatility of the player for the given variant/modus/type.
        /// </summary>
		/// <remarks>
		/// Asks on how How swingy is the player?
		/// A player who is inconsistent, has unpredictable results and changes performance quickly
		/// gets higher volatility and allows bigger rating changes.
		/// A stable consistent player gets lower volatility and small rating changes.
		/// </remarks>
        public double Sigma { get; set; } = Glicko2Constants.DefaultSigma;

        public double HighestRating { get; set; } = Glicko2Constants.DefaultRating;

		public double LowestRating { get; set; } = Glicko2Constants.DefaultRating;

		/// <summary>
		/// Gets or sets the amount of matches played by the given player for the given variant, type and modus.
		/// </summary>
		/// <remarks>
		/// The Glicko2 rating system needs atleast 10 matches played in order to calculate a proper rating value.
		/// </remarks>
		public int MatchesPlayed { get; set; } = 0;

        private string ConstructPK()
		{
			var factory = ItemFactoryCreator.Create<PlayerRatingItem>();
			return string.Format(factory.PKFormat, PlayerId);
		}

		private string ConstructSK()
		{
            var factory = ItemFactoryCreator.Create<PlayerRatingItem>();
            return string.Format(factory.SKFormat, Variant);
		}
	}
}
