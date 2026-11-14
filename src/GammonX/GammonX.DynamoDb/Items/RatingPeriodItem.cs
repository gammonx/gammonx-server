using Amazon.DynamoDBv2.DataModel;

using GammonX.DynamoDb.Stats;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
    /// <summary>
    /// Stores one matchs result and both players rating inputs before that result is applied.
    /// </summary>
    /// <remarks>
    /// The period is committed with the updated player rating, but retains the pre-match values.
    /// Use this matchs period as the baseline when comparing its pre-match rating to the current rating.
    /// </remarks>
    public class RatingPeriodItem
    {
        /// <summary>
        /// Gets a primary key like 'PLAYER#{PlayerId}'.
		/// </summary>
		[DynamoDBHashKey("PK")]
        public string PK => ConstructPK();

        /// <summary>
        /// Gets a sort key like 'MATCH#{Variant}#{Type}#{Modus}#{MatchId}'.
        /// </summary>
        [DynamoDBRangeKey("SK")]
        public string SK => ConstructSK();

        public string ItemType { get; } = ItemTypes.RatingPeriodItemType;

        public MatchVariant Variant { get; set; } = MatchVariant.Unknown;

        public MatchModus Modus { get; set; } = MatchModus.Unknown;

        public Models.Enums.MatchType Type { get; set; } = Models.Enums.MatchType.Unknown;

        /// <summary>
        /// Gets or sets the id of the related match.
        /// </summary>
        public Guid MatchId { get; set; } = Guid.Empty;

        /// <summary>
		/// Gets or sets the id of the <see cref="PlayerItem"/> this rating belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

        /// <summary>
		/// Gets or sets the id of the <see cref="PlayerItem"/> opponent this rating played against."/>
		/// </summary>
		public Guid OpponentId { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the normalized score for this player in the related match, from 0 to 1.
        /// A value of <c>0.6</c> is a normalized score, not a percentage value.
        /// </summary>
        public double MatchScore { get; set; } = 0;

        /// <summary>
        /// Gets or sets the players ordinary rating before applying this matchs result.
        /// </summary>
        public double PlayerRating { get; set; } = Glicko2Constants.DefaultRating;

        /// <summary>
        /// Gets or sets the players rating deviation before applying this matchs result.
        /// </summary>
        /// <remarks>
        /// Asks on how uncertain is the rating?
        /// RD ranges:
		/// - Low RD (trusted rating): 30–80
		/// - Medium RD: 80–150
		/// - High RD(not enough recent games): 150–350
		/// RD increases automatically when inactive (because uncertainty grows).
		/// </remarks>
        public double PlayerRatingDeviation { get; set; } = Glicko2Constants.DefaultRD;

        /// <summary>
        /// Gets or sets the players volatility before applying this matchs result.
        /// </summary>
		/// <remarks>
		/// Asks on how How swingy is the player?
		/// A player who is inconsistent, has unpredictable results and changes performance quickly
		/// gets higher volatility and allows bigger rating changes.
		/// A stable consistent player gets lower volatility and small rating changes.
		/// </remarks>
        public double PlayerSigma { get; set; } = Glicko2Constants.DefaultSigma;

        /// <summary>
        /// Gets or sets the opponents ordinary rating before applying this matchs result.
        /// </summary>
        public double OpponentRating { get; set; } = Glicko2Constants.DefaultRating;

        /// <summary>
        /// Gets or sets the opponents rating deviation before applying this matchs result.
        /// </summary>
        /// <remarks>
        /// Asks on how uncertain is the rating?
        /// RD ranges:
		/// - Low RD (trusted rating): 30–80
		/// - Medium RD: 80–150
		/// - High RD(not enough recent games): 150–350
		/// RD increases automatically when inactive (because uncertainty grows).
		/// </remarks>
        public double OpponentRatingDeviation { get; set; } = Glicko2Constants.DefaultRD;

        /// <summary>
        /// Gets or sets the opponents volatility before applying this matchs result.
        /// </summary>
		/// <remarks>
		/// Asks on how How swingy is the player?
		/// A player who is inconsistent, has unpredictable results and changes performance quickly
		/// gets higher volatility and allows bigger rating changes.
		/// A stable consistent player gets lower volatility and small rating changes.
		/// </remarks>
        public double OpponentSigma { get; set; } = Glicko2Constants.DefaultSigma;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        private string ConstructPK()
        {
            var factory = ItemFactoryCreator.Create<RatingPeriodItem>();
            return string.Format(factory.PKFormat, PlayerId);
        }

        private string ConstructSK()
        {
            var factory = ItemFactoryCreator.Create<RatingPeriodItem>();
            return string.Format(factory.SKFormat, Variant, Type, Modus, MatchId);
        }
    }
}
