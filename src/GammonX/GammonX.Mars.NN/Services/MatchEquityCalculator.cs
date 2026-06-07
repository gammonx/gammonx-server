using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides the capability to compute a match equity for a given <see cref="GameEquityModel"/>
    /// and a MET (match equity table).
    /// </summary>
    /// <seealso cref="https://bkgm.com/articles/GOL/demo/equity.htm"/>
    public static class MatchEquityCalculator
    {
        /// <summary>
        /// Gets the match equity table (MET) for a 15-point match. The rows represent the points away for the player to win,
        /// </summary>
        /// <remarks>
        /// y-axis: points away for the player to win (0 to 14)
        /// x-axis: points away for the opponent to win (0 to 14)
        /// </remarks>
        private static readonly double[,] MET =
        {
            {0.50, 0.70, 0.75, 0.83, 0.85, 0.90, 0.91, 0.94, 0.95, 0.97, 0.97, 0.98, 0.98, 0.99, 0.99},
            {0.30, 0.50, 0.60, 0.68, 0.75, 0.81, 0.85, 0.88, 0.91, 0.93, 0.94, 0.95, 0.96, 0.97, 0.98},
            {0.25, 0.40, 0.50, 0.59, 0.66, 0.71, 0.76, 0.80, 0.84, 0.87, 0.90, 0.92, 0.94, 0.95, 0.96},
            {0.17, 0.32, 0.41, 0.50, 0.58, 0.64, 0.70, 0.75, 0.79, 0.83, 0.86, 0.88, 0.90, 0.92, 0.93},
            {0.15, 0.25, 0.34, 0.42, 0.50, 0.57, 0.63, 0.68, 0.73, 0.77, 0.81, 0.84, 0.87, 0.89, 0.90},
            {0.10, 0.19, 0.29, 0.36, 0.43, 0.50, 0.56, 0.62, 0.67, 0.72, 0.76, 0.79, 0.82, 0.85, 0.87},
            {0.09, 0.15, 0.24, 0.30, 0.37, 0.44, 0.50, 0.56, 0.61, 0.66, 0.70, 0.74, 0.78, 0.81, 0.84},
            {0.06, 0.12, 0.20, 0.25, 0.32, 0.38, 0.44, 0.50, 0.55, 0.60, 0.65, 0.69, 0.73, 0.77, 0.80},
            {0.05, 0.09, 0.16, 0.21, 0.27, 0.33, 0.39, 0.45, 0.50, 0.55, 0.60, 0.64, 0.68, 0.72, 0.76},
            {0.03, 0.07, 0.13, 0.17, 0.23, 0.28, 0.34, 0.40, 0.45, 0.50, 0.55, 0.60, 0.64, 0.68, 0.71},
            {0.03, 0.06, 0.10, 0.14, 0.19, 0.24, 0.30, 0.35, 0.40, 0.45, 0.50, 0.55, 0.59, 0.63, 0.67},
            {0.02, 0.05, 0.08, 0.12, 0.16, 0.21, 0.26, 0.31, 0.36, 0.40, 0.45, 0.50, 0.54, 0.58, 0.62},
            {0.02, 0.04, 0.06, 0.10, 0.13, 0.18, 0.22, 0.27, 0.32, 0.36, 0.41, 0.46, 0.50, 0.54, 0.58},
            {0.01, 0.03, 0.05, 0.08, 0.11, 0.15, 0.19, 0.23, 0.28, 0.32, 0.37, 0.42, 0.46, 0.50, 0.54},
            {0.01, 0.02, 0.04, 0.07, 0.10, 0.13, 0.16, 0.20, 0.24, 0.29, 0.33, 0.38, 0.42, 0.46, 0.50}
        };

        public static double CalculateEquity(
            GameEquityModel model,
            int pointsAway,
            int pointsAwayOpp,
            int cubeValue)
        {
            // we determine match score after each outcome
            var pa = pointsAway;
            var paOpp = pointsAwayOpp;
            var cube = cubeValue;

            // we clamp the values to 0
            var paWin = Math.Max(0, pa - cube);
            var paGammonWin = Math.Max(0, pa - 2 * cube);
            var paBgWin = Math.Max(0, pa - 3 * cube);
            var paOppWin = Math.Max(0, paOpp - cube);
            var paOppGammonWin = Math.Max(0, paOpp - 2 * cube);
            var paOppBgWin = Math.Max(0, paOpp - 3 * cube);

            // we calculate the equity for each outcome and sum them up
            var equity =
                model.WinSingleP * GetMET(paWin, paOpp)
                + model.WinGammonP * GetMET(paGammonWin, paOpp)
                + model.WinBackgammonP * GetMET(paBgWin, paOpp)
                + model.LoseSingleP * GetMET(pa, paOppWin)
                + model.LoseGammonP * GetMET(pa, paOppGammonWin)
                + model.LoseBackgammonP * GetMET(pa, paOppBgWin);

            return equity;
        }

        public static double GetMET(int whiteAway, int blackAway)
        {
            if (whiteAway <= 0) return 1.0;
            if (blackAway <= 0) return 0.0;

            whiteAway = Math.Min(whiteAway, 15);
            blackAway = Math.Min(blackAway, 15);

            return MET[whiteAway - 1, blackAway - 1];
        }
    }
}
