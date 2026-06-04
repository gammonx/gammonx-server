using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training
{
    /// <summary>
    /// Records (features, outcome) pairs during self-play for NN training.
    /// Each position is labelled with the terminal game outcome from the active
    /// player's perspective: 1.0 = that player won, 0.0 = that player lost.
    /// </summary>
    public sealed class SelfPlayRecorder
    {
        public const float DefaultLambda = 0.7f;

        private readonly IFeatureVectorExtractor _extractor;
        private readonly INeuralEvalService? _neuralEvalService; // null = generation 0
        private readonly float _lambda;
        private readonly List<(float[] Features, bool IsWhite, float[] NetPrediction)> _positions = [];

        public SelfPlayRecorder(IFeatureVectorExtractor extractor, INeuralEvalService? neuralEvalService = null, float lambda = DefaultLambda)
        {
            _extractor = extractor;
            _lambda = lambda;
            _neuralEvalService = neuralEvalService;
        }

        /// <summary>
        /// Records the resulting board features after a move was played.
        /// Features are already in active-player perspective so white and black
        /// positions are directly comparable — no separate recorder per color needed.
        /// </summary>
        /// <param name="model">Normalized features of the resulting board state.</param>
        /// <param name="board">Target board.</param>
        /// <param name="isWhite"><c>true</c> if the active player this turn was white.</param>
        public void RecordPosition(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var features = _extractor.Extract(model, board, isWhite);
            // we store the networks current prediction for this state (0.5 if no net yet)
            var netPred = _neuralEvalService?.Predict(model, board, isWhite) ?? [0.5f, 0.0f, 0.0f, 0.0f, 0.0f];
            _positions.Add((features, isWhite, netPred));
        }

        /// <summary>
        /// Returns the raw network predictions collected during the game.
        /// Only meaningful when a neural eval service is present; otherwise all values are 0.5.
        /// </summary>
        public IReadOnlyList<float[]> NetPredictions => _positions.Select(p => p.NetPrediction).ToList();

        /// <summary>
        /// Finalizes the recording and returns all (features, label) training samples.
        /// Each position receives the terminal game outcome from the active player's perspective.
        /// </summary>
        /// <param name="winnerResult">The result from the winner's perspective.</param>
        /// <param name="loserResult">The result from the loser's perspective.</param>
        /// <param name="whiteWon">Whether white won the game.</param>
        public IReadOnlyList<(float[] Features, float[] Label)> Finalize(GameResult winnerResult, GameResult loserResult, bool whiteWon)
        {
            int T = _positions.Count;
            var result = new List<(float[] Features, float[] Label)>(T);

            for (int t = 0; t < T; t++)
            {
                var (features, isWhite, _) = _positions[t];

                // we determine this positions outcome from the active players perspective
                bool activePlayerWon = isWhite == whiteWon;
                var gameResult = activePlayerWon ? winnerResult : loserResult;

                var pWin = gameResult switch
                {
                    GameResult.Single => 1.0f,
                    GameResult.Gammon => 1.0f,
                    GameResult.Backgammon => 1.0f,
                    GameResult.DoubleDeclined => 1.0f,
                    GameResult.Resign => 1.0f,
                    GameResult.Draw => 0.5f,
                    _ => 0.0f
                };
                var pGammonWin = gameResult switch
                {
                    GameResult.Gammon => 1.0f,
                    GameResult.Backgammon => 1.0f,
                    GameResult.DoubleDeclined => 1.0f,
                    GameResult.Resign => 1.0f,
                    GameResult.Draw => 0.5f,
                    _ => 0.0f
                };
                var pBackgammonWin = gameResult switch
                {
                    GameResult.Backgammon => 1.0f,
                    GameResult.Draw => 0.5f,
                    _ => 0.0f
                };
                var pGammonLoss = gameResult switch
                {
                    GameResult.LostGammon => 1.0f,
                    GameResult.LostBackgammon => 1.0f,
                    GameResult.LostDoubleDeclined => 1.0f,
                    GameResult.LostResign => 1.0f,
                    GameResult.Draw => 0.5f,
                    _ => 0.0f
                };
                var pBackgammonLoss = gameResult switch
                {
                    GameResult.LostBackgammon => 1.0f,
                    GameResult.Draw => 0.5f,
                    _ => 0.0f
                };

                // near the end we trust terminal
                // early we trust next-state bootstrap (try to exclude noisy game start)
                int stepsFromEnd = T - 1 - t;
                float decay = MathF.Pow(_lambda, stepsFromEnd);

                // we make next-state network prediction from the same players perspective
                // if no future same-player position exists (last 2 turns), bootstrap from terminal
                float bootstrap = (t + 2 < T)
                    ? _positions[t + 2].NetPrediction[0]
                    : pWin;

                float label = decay * pWin + (1f - decay) * bootstrap;
                result.Add((features, [label, pGammonWin, pBackgammonWin, pGammonLoss, pBackgammonLoss]));
            }

            return result;
        }
    }
}
