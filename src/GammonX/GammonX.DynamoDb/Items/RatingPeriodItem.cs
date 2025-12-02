using Amazon.DynamoDBv2.DataModel;

using GammonX.DynamoDb.Stats;
using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
    public class RatingPeriodItem
    {
        /// <summary>
		/// Gets a primary key like 'PLAYER#{matchId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
        public string PK => ConstructPK();

        /// <summary>
        /// Gets a sort key like 'MATCH#{variant}#{type}#{modus}#{matchId}'.
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
        /// Gets or sets the clamped match score of the related match.
        /// </summary>
        public double MatchScore { get; set; } = 0;

        /// <summary>
        /// Gets or sets the ordinary rating of the player for the given variant/modus/type.
        /// </summary>
        public double PlayerRating { get; set; } = Glicko2Constants.DefaultRating;

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
        public double PlayerRatingDeviation { get; set; } = Glicko2Constants.DefaultRD;

        /// <summary>
        /// Gets or sets the volatility of the player for the given variant/modus/type.
        /// </summary>
		/// <remarks>
		/// Asks on how How swingy is the player?
		/// A player who is inconsistent, has unpredictable results and changes performance quickly
		/// gets higher volatility and allows bigger rating changes.
		/// A stable consistent player gets lower volatility and small rating changes.
		/// </remarks>
        public double PlayerSigma { get; set; } = Glicko2Constants.DefaultSigma;

        /// <summary>
        /// Gets or sets the ordinary rating of the opponent for the given variant/modus/type.
        /// </summary>
        public double OpponentRating { get; set; } = Glicko2Constants.DefaultRating;

        /// <summary>
        /// Gets or sets the rating deviation of the opponent for the given variant/modus/type.
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
        /// Gets or sets the volatility of the opponent for the given variant/modus/type.
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
            return string.Format(factory.SKFormat, Variant, Type, Modus);
        }
    }
}
