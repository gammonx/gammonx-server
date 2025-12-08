using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Stats
{
    /// <summary>
    /// Provides the glicko2 rating (unscaled).
    /// </summary>
    internal sealed class Glicko2Rating
    {
        /// <summary>
        /// Gets the scaled rating μ (Mu).
        /// </summary>
        /// <remarks>
        ///  Relates to <see cref="PlayerRatingItem.Rating"/>.
        /// </remarks>
        public double Mu { get; }

        /// <summary>
        /// Gets the the scaled rating deviation (uncertainty) φ (Phi).
        /// </summary>
        /// <remarks>
        ///  Relates to <see cref="PlayerRatingItem.RatingDeviation"/>.
        /// </remarks>
        public double Phi { get; }

        /// <summary>
        /// Gets the volatility (how variable the player's performance is) σ (Sigma)
        /// </summary>
        /// <remarks>
        ///  Relates to <see cref="PlayerRatingItem.Sigma"/>.
        /// </remarks>
        public double Sigma { get; }

        public Glicko2Rating(double mu, double phi, double sigma)
        {
            Mu = mu;
            Phi = phi;
            Sigma = sigma;
        }

        public static Glicko2Rating From(PlayerRatingItem playerRating)
        {
            return new Glicko2Rating(
                Glicko2RatingCalculator.ToMu(playerRating.Rating), 
                Glicko2RatingCalculator.ToPhi(playerRating.RatingDeviation), 
                playerRating.Sigma);
        }

        public override string ToString() => $"μ={Mu:F2}, φ={Phi:F2}, σ={Sigma:F4}";
    }
}
