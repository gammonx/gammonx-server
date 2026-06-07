namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Provides the capability to compute a game equity for a given <see cref="GameOutcomeModel"/>.
    /// </summary>
    public class GameEquityModel
    {
        /// <summary>
        /// Gets the probability of winning the game with a single point.
        /// </summary>
        public double WinSingleP { get; private set; }

        /// <summary>
        /// Gets the probability of winning the game with a gammon.
        /// </summary>
        public double WinGammonP { get; private set; }

        /// <summary>
        /// Gets the probability of winning the game with a backgammon.
        /// </summary>
        public double WinBackgammonP { get; private set; }

        /// <summary>
        /// Gets the probability of losing the game with a single point.
        /// </summary>
        public double LoseSingleP { get; private set; }

        /// <summary>
        /// Gets the probability of losing the game with a gammon.
        /// </summary>
        public double LoseGammonP { get; private set; }

        /// <summary>
        /// Gets the probability of losing the game with a backgammon.
        /// </summary>
        public double LoseBackgammonP { get; private set; }

        /// <summary>
        /// Gets the computed game equity.
        /// </summary>
        public double Equity { get; private set; }

        public GameEquityModel(GameOutcomeModel outcome)
        {
            WinSingleP = Math.Max(0, outcome.WinP - outcome.WinGammonP);
            WinGammonP = Math.Max(0, outcome.WinGammonP - outcome.WinBackgammonP);
            WinBackgammonP = Math.Max(0, outcome.WinBackgammonP);
            LoseSingleP = Math.Max(0, outcome.LoseP - outcome.LoseGammonP);
            LoseGammonP = Math.Max(0, outcome.LoseGammonP - outcome.LoseBackgammonP);
            LoseBackgammonP = Math.Max(0, outcome.LoseBackgammonP);
            Equity = CalculateEquity(this);
        }

        private static double CalculateEquity(GameEquityModel model)
        {
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
