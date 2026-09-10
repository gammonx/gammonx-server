namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Converts cumulative game-outcome probabilities into atomic results and computes their expected equity.
    /// </summary>
    /// <remarks>
    /// The input probabilities are projected into the valid constrained space before they are decomposed.
    /// For example, single-win probability is P(win) - P(gammon win), while gammon-win probability is
    /// P(gammon win) - P(backgammon win). Equity uses weights of 1, 2, and 3 for single, gammon, and
    /// backgammon results, with losses contributing negative values.
    /// </remarks>
    public class GameEquityModel
    {
        /// <summary>
        /// Gets the probability of winning the game with a single point.
        /// </summary>
        public double WinSingleP { get; }

        /// <summary>
        /// Gets the probability of winning the game with a gammon.
        /// </summary>
        public double WinGammonP { get; }

        /// <summary>
        /// Gets the probability of winning the game with a backgammon.
        /// </summary>
        public double WinBackgammonP { get; }

        /// <summary>
        /// Gets the probability of losing the game with a single point.
        /// </summary>
        public double LoseSingleP { get; }

        /// <summary>
        /// Gets the probability of losing the game with a gammon.
        /// </summary>
        public double LoseGammonP { get; }

        /// <summary>
        /// Gets the probability of losing the game with a backgammon.
        /// </summary>
        public double LoseBackgammonP { get; }

        /// <summary>
        /// Gets the cubeless game equity: the expected number of points won or lost at cube value 1, in the range [-3, 3].
        /// </summary>
        /// <summary>
        /// Gets the expected game equity, where wins are positive and losses are negative.
        /// Backgammon Formula = E = P(SW) + 2P(GW) + 3P(BGW) − P(SL) − 2P(GL) − 3P(BGL)
        /// </summary>
        public double Equity { get; private set; }

        /// <summary>
        /// Gets the constraint-validation report for the original outcome.
        /// </summary>
        public GameOutcomeConstraintReport ConstraintReport { get; }

        /// <summary>
        /// Gets whether the input outcome required projection before decomposition.
        /// </summary>
        public bool WasProjected { get; }

        /// <summary>
        /// Gets the total absolute correction applied during projection.
        /// </summary>
        public double ProjectionMagnitude { get; }

        /// <summary>
        /// Gets the sum of all six mutually exclusive atomic outcome probabilities.
        /// </summary>
        /// <remarks>A value close to 1 indicates that the projected win and loss probabilities fully partition the outcome space.</remarks>
        public double AtomicProbabilityMass { get; }

        /// <summary>
        /// Initializes an equity model from cumulative outcome probabilities.
        /// </summary>
        /// <param name="outcome">The outcome probabilities to validate, project, and convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="outcome"/> is null.</exception>
        public GameEquityModel(GameOutcomeModel outcome)
        {
            ArgumentNullException.ThrowIfNull(outcome);

            ConstraintReport = outcome.ConstraintReport;
            var projected = GameOutcomeConstraintValidator.Project(outcome);
            ProjectionMagnitude = projected.CorrectionMagnitude;
            WasProjected = projected.WasProjected;

            // Convert cumulative probabilities into mutually exclusive outcome probabilities.
            WinSingleP = projected.WinP - projected.WinGammonP;
            WinGammonP = projected.WinGammonP - projected.WinBackgammonP;
            WinBackgammonP = projected.WinBackgammonP;
            LoseSingleP = projected.LoseP - projected.LoseGammonP;
            LoseGammonP = projected.LoseGammonP - projected.LoseBackgammonP;
            LoseBackgammonP = projected.LoseBackgammonP;
            AtomicProbabilityMass = WinSingleP
                + WinGammonP
                + WinBackgammonP
                + LoseSingleP
                + LoseGammonP
                + LoseBackgammonP;
            Equity = CalculateEquity(this);
        }

        /// <summary>
        /// Calculates expected equity (cubeless game points) by weighting each atomic result by its game value.
        /// </summary>
        private static double CalculateEquity(GameEquityModel model)
        {
            // Single, gammon, and backgammon outcomes are worth one, two, and three points respectively.
            var equity = model.WinSingleP * 1
                       + model.WinGammonP * 2
                       + model.WinBackgammonP * 3
                       - model.LoseSingleP * 1
                       - model.LoseGammonP * 2
                       - model.LoseBackgammonP * 3;
            return equity;
        }
    }
}
