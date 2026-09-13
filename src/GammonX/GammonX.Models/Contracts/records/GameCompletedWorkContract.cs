using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
    /// Provides both player views required to persist one completed game.
    /// </summary>
    [DataContract]
    public class GameCompletedWorkContract
    {
        [DataMember(Name = "Records")]
        public GameRecordContract[] Records { get; set; } = [];

        /// <summary>
        /// Gets the validated game records, ensuring there is a winner and a loser game record pairs.
        /// </summary>
        /// <returns>Validated game records as a tuple containing the winner and loser.</returns>
        public (GameRecordContract Winner, GameRecordContract Loser) GetValidatedRecords()
        {
            return WorkContractValidation.ValidateGameRecords(Records);
        }
    }
}