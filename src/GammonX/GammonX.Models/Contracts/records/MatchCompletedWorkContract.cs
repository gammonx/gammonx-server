using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
    /// Provides both player views required to persist one completed match.
    /// </summary>
    [DataContract]
    public class MatchCompletedWorkContract
    {
        [DataMember(Name = "Records")]
        public MatchRecordContract[] Records { get; set; } = [];

        /// <summary>
        /// Gets the validated match records, ensuring there is a winner and a loser match record pairs.
        /// </summary>
        /// <returns>Validated match records as a tuple containing the winner and loser.</returns>
        public (MatchRecordContract Winner, MatchRecordContract Loser) GetValidatedRecords()
        {
            return WorkContractValidation.ValidateMatchRecords(Records, false);
        }
    }
}