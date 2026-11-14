using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
    /// Provides a minimum contract for player rating response.
    /// </summary>
    [DataContract]
    public sealed class PlayerRatingResponseContract : BaseResponseContract
    {
        /// <summary>
        /// Gets or sets the current rating, including all committed updates.
        /// </summary>
        [DataMember(Name = "Rating")]
        public double Rating { get; set; } = 0;

        /// <summary>
        /// Gets or sets the rating before the requested match, captured in that match's rating period.
        /// Null when no matchId was requested or its period has not been committed.
        /// </summary>
        [DataMember(Name = "PreviousRating", EmitDefaultValue = false)]
        public double? PreviousRating { get; set; }

        /// <summary>
        /// Gets or sets Rating minus PreviousRating: positive for a gain, negative for a loss.
        /// Null when the baseline is unavailable. Includes later matches if they have updated Rating.
        /// </summary>
        [DataMember(Name = "RatingChange", EmitDefaultValue = false)]
        public double? RatingChange { get; set; }
    }
}
