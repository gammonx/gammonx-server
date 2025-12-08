using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Stats
{
    /// <summary>
    /// Input data structure for <see cref="MatchScoreCalculator.Calculate(Guid, MatchScoreInput, MatchScoreInput)"/>.
    /// </summary>
    internal struct MatchScoreInput
    {
        /// <summary>
        /// Gets or sets the player id.
        /// </summary>
        public Guid PlayerId { get; set; }

        /// <summary>
        /// Gets or sets the result of the match.
        /// </summary>
        public MatchResult Result { get; set; }

        /// <summary>
        /// Gets or sets the amount of gammons the related player scored.
        /// </summary>
        public int Gammons { get; set; }

        /// <summary>
        /// Gets or sets the amount of backgammons the related player scored.
        /// </summary>
        public int Backgammons { get; set; }

        /// <summary>
        /// Gets or sets the average pipes left for the related player for lost games..
        /// </summary>
        public double AvgPipesLeft { get; set; }

        /// <summary>
        /// Gets or sets the amount of points score in the match.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Gets or sets the amount of games played in the match.
        /// </summary>
        public int Length { get; set; }
    }
}
