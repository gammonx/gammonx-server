namespace GammonX.DynamoDb.Stats
{
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
        /// <param name="player">Player rating to update.</param>
        /// <param name="opponent">Opponent rating to analyze.</param>
        /// <param name="matchScore">Match score.</param>
        /// <returns>Updted Glicko2 rating.</returns>
        public static Glicko2Rating Update(Glicko2Rating player, Glicko2Rating opponent, double matchScore)
        {
            if (FromMu(opponent.Mu) == Glicko2Constants.DefaultRating && FromPhi(opponent.Phi) == Glicko2Constants.DefaultRD)
            {
                // no games and RD increases due to decay
                double tmpPhiPrime = Math.Sqrt(player.Phi * player.Phi + player.Sigma * player.Sigma);
                return new Glicko2Rating(player.Mu, tmpPhiPrime, player.Sigma);
            }

            var s = matchScore;
            var muOpp = opponent.Mu;
            var phiOpp = opponent.Phi;

            // STEP 2 :: compute variance v
            double vInv = g(phiOpp) * g(phiOpp) * E(player.Mu, muOpp, phiOpp) * (1 - E(player.Mu, muOpp, phiOpp));
            double v = 1.0 / vInv;

            // STEP 3 :: compute Δ
            double delta = v * g(phiOpp) * (s - E(player.Mu, muOpp, phiOpp));

            // STEP 4 :: new volatility (sigma σ')
            double a = Math.Log(player.Sigma * player.Sigma);
            double A = a;
            double B;

            if (delta * delta > (player.Phi * player.Phi + v))
            {
                B = Math.Log(delta * delta - player.Phi * player.Phi - v);
            }
            else
            {
                int k = 1;
                B = a - k * Glicko2Constants.Tau;

                while (f(B, delta, player.Phi, v, a) < 0)
                {
                    k++;
                    B = a - k * Glicko2Constants.Tau;

                    if (k > 1000)
                        throw new Exception("Glicko2 volatility iteration failed to converge.");
                }
            }

            double fA = f(A, delta, player.Phi, v, a);
            double fB = f(B, delta, player.Phi, v, a);

            // STEP 5 :: safe volatility iteration (binary search)
            while (Math.Abs(B - A) > 0.000001)
            {
                double C = (A + B) / 2;
                double fC = f(C, delta, player.Phi, v, a);

                if (fC * fA < 0)
                {
                    B = C;
                    fB = fC;
                }
                else
                {
                    A = C;
                    fA = fC;
                }
            }

            double sigmaPrime = Math.Exp(A / 2);

            // add volatility floor keeps the system responsive
            // this ensures that a players rating does not freeze at some point
            sigmaPrime = Math.Max(sigmaPrime, 0.03);

            // STEP 6 :: pre-new phi*
            double phiStar = Math.Sqrt(player.Phi * player.Phi + sigmaPrime * sigmaPrime);

            // STEP 7 :: new phi
            double phiPrime = 1.0 / Math.Sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);

            // STEP 8 :: new mu
            double muPrime = player.Mu + phiPrime * phiPrime * g(phiOpp) * (s - E(player.Mu, muOpp, phiOpp));

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
