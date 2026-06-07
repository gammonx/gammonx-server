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

        public GameOutcomeModel(float[] netPredictions)
        {
            WinP = netPredictions[0];
            WinGammonP = netPredictions[1];
            WinBackgammonP = netPredictions[2];
            LoseGammonP = netPredictions[3];
            LoseBackgammonP = netPredictions[4];
        }
    }
}
