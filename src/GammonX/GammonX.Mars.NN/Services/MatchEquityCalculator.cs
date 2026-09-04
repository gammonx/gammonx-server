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
        private static readonly double[,] MET = KazarossXg2Met.Values;

        public static double CalculateEquity(
            GameEquityModel model,
            int pointsAway,
            int pointsAwayOpp,
            int cubeValue)
        {
            // if points are 0 or less, the match is already lost
            if (pointsAway <= 0) return 1.0;
            if (pointsAwayOpp <= 0) return 0.0;

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
                // from opponents perspective
                + model.LoseSingleP * GetMET(pa, paOppWin)
                + model.LoseGammonP * GetMET(pa, paOppGammonWin)
                + model.LoseBackgammonP * GetMET(pa, paOppBgWin);

            return equity;
        }

        /// <summary>
        /// Calculates the current player's match equity after an accepted double.
        /// </summary>
        /// <remarks>
        /// This follows bglab's <c>tp_gammons()</c> automatic-recubing rule. Normally the accepted double uses
        /// <c>2 * cubeValue</c>; when the non-taking player is at or below <c>2 * cubeValue</c>, the effective cube is
        /// <c>4 * cubeValue</c>. For example, a take at 4-away versus 2-away with cube 1 uses an effective cube of 4.
        /// </remarks>
        /// <param name="model">The current player's discrete game outcome probabilities.</param>
        /// <param name="pointsAway">The current player's points away.</param>
        /// <param name="pointsAwayOpp">The opponent's points away.</param>
        /// <param name="cubeValue">The cube value before the double.</param>
        /// <param name="currentPlayerIsTaker">Whether the current player is accepting the double.</param>
        /// <returns>The current player's match equity after the accepted double.</returns>
        public static double CalculateEquityAfterAcceptedDouble(
            GameEquityModel model,
            int pointsAway,
            int pointsAwayOpp,
            int cubeValue,
            bool currentPlayerIsTaker)
        {
            var recubeTriggerPointsAway = currentPlayerIsTaker
                ? pointsAwayOpp
                : pointsAway;
            var effectiveCubeValue = cubeValue * 2;

            if (recubeTriggerPointsAway <= cubeValue * 2)
            {
                effectiveCubeValue *= 2;
            }

            return CalculateEquity(
                model,
                pointsAway,
                pointsAwayOpp,
                effectiveCubeValue);
        }

        /// <summary>
        /// Gets a match equity table value for the supplied points-away values.
        /// </summary>
        /// <param name="playerAway">The points away for the player.</param>
        /// <param name="opponentAway">The points away for the opponent.</param>
        /// <returns>The player's match equity.</returns>
        public static double GetMET(int playerAway, int opponentAway)
        {
            if (playerAway <= 0) return 1.0;
            if (opponentAway <= 0) return 0.0;

            var maxPointsAway = MET.GetLength(0);
            playerAway = Math.Min(playerAway, maxPointsAway);
            opponentAway = Math.Min(opponentAway, maxPointsAway);

            return MET[playerAway - 1, opponentAway - 1];
        }
    }
}
