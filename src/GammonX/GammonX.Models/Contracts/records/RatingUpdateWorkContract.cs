using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
    /// Provides both player views required to update ratings for one completed ranked match.
    /// </summary>
    [DataContract]
    public class RatingUpdateWorkContract
    {
        [DataMember(Name = "Records")]
        public MatchRecordContract[] Records { get; set; } = [];

        /// <summary>
        /// Gets the validated match records for a completed ranked match, ensuring there is a winner and a loser match record pairs.
        /// </summary>
        /// <returns>Validated match records as a tuple containing the winner and loser.</returns>
        public (MatchRecordContract Winner, MatchRecordContract Loser) GetValidatedRecords()
        {
            return WorkContractValidation.ValidateMatchRecords(Records, true);
        }
    }
}