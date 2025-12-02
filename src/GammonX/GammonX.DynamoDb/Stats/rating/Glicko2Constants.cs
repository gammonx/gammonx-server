namespace GammonX.DynamoDb.Stats
{
    internal static class Glicko2Constants
    {
        /// <summary>
        /// Controls volatility change. 0.3–1.2 is typical.
        /// </summary>
        /// <remarks>
        /// A higher tau means that volatility can change more rapidly.
        /// A lower tau means that volatility changes more slowly.
        /// </remarks>
        public const double Tau = 0.8;

        /// <summary>
        /// Gets the default rating for new players.
        /// </summary>
        public const double DefaultRating = 1200;

        /// <summary>
        /// Gets the default rating deviation for new players.
        /// </summary>
        public const double DefaultRD = 350;

        /// <summary>
        /// Gets the default volatility for new players.
        /// </summary>
        public const double DefaultSigma = 0.06;

        /// <summary>
        /// Convert between Glicko and Glicko-2 scales
        /// </summary>
        public const double Scale = 173.7178;

        /// <summary>
        /// Gets the number of matches per rating period.
        /// </summary>
        public const int RatingPeriod = 30;

        /// <summary>
        /// Gets the max loop iterations for volatility calculation.
        /// </summary>
        public const int MaxIterations = 1000;
    }
}
