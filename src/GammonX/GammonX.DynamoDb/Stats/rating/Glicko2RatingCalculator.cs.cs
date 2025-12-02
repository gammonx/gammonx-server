using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Stats
{
    /// <summary>
    /// Provides a .net implementation of the Glicko2 rating system.
    /// </summary>
    /// <seealso cref="https://www.glicko.net/glicko/glicko2.pdf"/>
    internal static class Glicko2RatingCalculator
    {
        /// <summary>
        /// Converts 1500-based values to the Glicko2 scale.
        /// </summary>
        /// <param name="rating">Rating to convert.</param>
        /// <returns>Returns rating to glicko2 scale.</returns>
        public static double ToMu(double rating) => (rating - Glicko2Constants.DefaultRating) / Glicko2Constants.Scale;

        public static double ToPhi(double rd) => rd / Glicko2Constants.Scale;

        public static double FromMu(double mu) => mu * Glicko2Constants.Scale + Glicko2Constants.DefaultRating;

        public static double FromPhi(double phi) => phi * Glicko2Constants.Scale;

        /// <summary>
        /// Updates the rating of the given <paramref name="player"/>.
        /// </summary>
        /// <remarks>
        /// The rating is not peristed yet to the database. Instead it just returns the updated rating instance.
        /// </remarks>
        /// <param name="player">Player rating to update.</param>
        /// <param name="ratingPeriodItems">Rating periods to include.</param>
        /// <returns>Updted Glicko2 rating.</returns>
        public static Glicko2Rating Calculate(Glicko2Rating player, params RatingPeriodItem[] ratingPeriodItems)
        {
            var periods = ratingPeriodItems.ToList();

            if (periods.Count == 0)
            {
                // no matches played in the given rating period > increase uncertainty
                double phiPrimeD = Math.Sqrt(player.Phi * player.Phi + player.Sigma * player.Sigma);
                return new Glicko2Rating(player.Mu, phiPrimeD, player.Sigma);
            }

            // STEP 1 :: convert opponent ratings
            var converted = ratingPeriodItems
                .Select(r => (muJ: ToMu(r.OpponentRating), phiJ: ToPhi(r.OpponentRatingDeviation), s: r.MatchScore))
                .ToList();

            // STEP 2 :: compute variance v
            double vInv = converted.Sum(r => g(r.phiJ) * g(r.phiJ) * E(player.Mu, r.muJ, r.phiJ) * (1 - E(player.Mu, r.muJ, r.phiJ)));
            double v = 1.0 / vInv;

            // STEP 3 :: compute Δ
            double delta = v * converted.Sum(r => g(r.phiJ) * (r.s - E(player.Mu, r.muJ, r.phiJ)));

            // STEP 4 :: new volatility (sigma σ')
            double a = Math.Log(player.Sigma * player.Sigma);
            double A = a;
            double B;

            if (delta * delta > player.Phi * player.Phi + v)
            {
                B = Math.Log(delta * delta - player.Phi * player.Phi - v);
            }
            else
            {
                int k = 1;
                B = a - k * Math.Sqrt(Glicko2Constants.Tau * Glicko2Constants.Tau);
                while (f(B, delta, player.Phi, v, a) < 0)
                {
                    k++;
                    B = a - k * Math.Sqrt(Glicko2Constants.Tau * Glicko2Constants.Tau);
                }
            }

            double fA = f(A, delta, player.Phi, v, a);
            double fB = f(B, delta, player.Phi, v, a);

            var iteration = 0;
            while (Math.Abs(B - A) > 1e-6 && iteration < Glicko2Constants.MaxIterations)
            {
                double C = A + (A - B) * fA / (fB - fA);
                double fC = f(C, delta, player.Phi, v, a);

                if (fC * fB < 0)
                {
                    A = B;
                    fA = fB;
                }
                else
                {
                    fA = fA / 2;
                }

                B = C;
                fB = fC;

                iteration++;
            }

            double sigmaPrime = Math.Exp(A / 2);

            // add volatility floor keeps the system responsive
            // this ensures that a players rating does not freeze at some point
            sigmaPrime = Math.Max(sigmaPrime, 0.03);

            // STEP 5 :: pre-new phi*
            double phiStar = Math.Sqrt(player.Phi * player.Phi + sigmaPrime * sigmaPrime);

            // STEP 6 :: new phi
            double phiPrime = 1.0 / Math.Sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);

            // STEP 7 :: new mu
            double muPrime = player.Mu + phiPrime * phiPrime * converted.Sum(r => g(r.phiJ) * (r.s - E(player.Mu, r.muJ, r.phiJ)));

            return new Glicko2Rating(muPrime, phiPrime, sigmaPrime);
        }

        /// <summary>
        /// Gets g(phi) function.
        /// </summary>
        /// <param name="phi">phi parameter.</param>
        /// <returns>Returns g.</returns>
        private static double g(double phi) => 1.0 / Math.Sqrt(1.0 + 3.0 * phi * phi / (Math.PI * Math.PI));

        private static double E(double mu, double muJ, double phiJ)
        {
            // expected score
            return 1.0 / (1.0 + Math.Exp(-g(phiJ) * (mu - muJ)));
        }

        private static double f(double x, double delta, double phi, double v, double a)
        {
            double ex = Math.Exp(x);
            double num = ex * (delta * delta - phi * phi - v - ex);
            double den = 2.0 * (phi * phi + v + ex) * (phi * phi + v + ex);
            return num / den - (x - a) / (Glicko2Constants.Tau * Glicko2Constants.Tau);
        }
    }
}
