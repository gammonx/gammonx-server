namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Provides the game outcome probabilities.
    /// </summary>
    public class GameOutcomeModel
    {
        /// <summary>
        /// Gets the probability of winning the game with a single point, gammon or backgammon.
        /// </summary>
        public double WinP { get; }

        /// <summary>
        /// Gets the probability of winning the game with a gammon or backgammon.
        /// </summary>
        public double WinGammonP { get; }

        /// <summary>
        /// Gets the probability of winning the game with a backgammon.
        /// </summary>
        public double WinBackgammonP { get; }

        /// <summary>
        /// Gets the probability of losing the game with a single point, gammon or backgammon.
        /// </summary>
        public double LoseP => 1 - WinP;

        /// <summary>
        /// Gets the probability of losing the game with a gammon or backgammon.
        /// </summary>
        public double LoseGammonP { get; }

        /// <summary>
        /// Gets the probability of losing the game with a backgammon.
        /// </summary>
        public double LoseBackgammonP { get; }

        public GameOutcomeConstraintReport ConstraintReport => GameOutcomeConstraintValidator.Validate(this);

        /// <summary>
        /// Maps physical network outputs to the shared cumulative outcome order.
        /// Three-head models have no backgammons. their third head is gammon loss.
        /// This mapping does not project probabilities, preserving diagnostic violations.
        /// </summary>
        /// <param name="predictions">Neural net probability predictions.</param>
        /// <returns>Returns a shared 5 label array of predictions (pWin, gWin, bgWin, gLose, bgLose).</returns>
        public static float[] ToCanonical(ReadOnlySpan<float> predictions) => predictions.Length switch
        {
            1 => [predictions[0], 0f, 0f, 0f, 0f],
            3 => [predictions[0], predictions[1], 0f, predictions[2], 0f],
            5 => predictions.ToArray(),
            _ => throw new ArgumentException("Expected one, three, or five outcome heads.", nameof(predictions))
        };

        public GameOutcomeModel(float[] netPredictions)
        {
            ArgumentNullException.ThrowIfNull(netPredictions);

            if (netPredictions.Length < GameOutcomeConstraintValidator.FullHeadCount)
            {
                throw new ArgumentException("Five outcome heads are required.", nameof(netPredictions));
            }

            WinP = netPredictions[0];
            WinGammonP = netPredictions[1];
            WinBackgammonP = netPredictions[2];
            LoseGammonP = netPredictions[3];
            LoseBackgammonP = netPredictions[4];
        }
    }
}
