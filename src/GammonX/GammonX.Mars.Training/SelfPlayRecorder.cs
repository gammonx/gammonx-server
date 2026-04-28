using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;
using GammonX.Mars.Server.Services.NN;

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
        private readonly List<(float[] Features, bool IsWhite, float NetPrediction)> _positions = [];

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
        /// <param name="eval">Normalized features of the resulting board state.</param>
        /// <param name="isWhite"><c>true</c> if the active player this turn was white.</param>
        public void RecordPosition(NormalizedEvalResultModel eval, bool isWhite)
        {
            var features = _extractor.Extract(eval);
            // we store the networks current prediction for this state (0.5 if no net yet)
            var netPred = _neuralEvalService?.Predict(eval) ?? 0.5f;
            _positions.Add((features, isWhite, netPred));
        }

        /// <summary>
        /// Finalizes the recording and returns all (features, label) training samples.
        /// Each position receives the terminal game outcome from the active player's perspective:
        /// 1.0 if that player won, 0.0 if they lost.
        /// </summary>
        /// <param name="whiteWon"><c>true</c> if white won the game.</param>
        public IReadOnlyList<(float[] Features, float Label)> Finalize(bool whiteWon)
        {
            int T = _positions.Count;
            var result = new List<(float[] Features, float Label)>(T);

            for (int t = 0; t < T; t++)
            {
                var (features, isWhite, _) = _positions[t];
                var terminal = (isWhite == whiteWon) ? 1.0f : 0.0f;

                // near end we trust terminal
                // early we trust next-state bootstrap (try to exclude noisy game start)
                int stepsFromEnd = T - 1 - t;
                float decay = MathF.Pow(_lambda, stepsFromEnd);

                // we make next-state network prediction from the same players perspective
                // if no future same-player position exists (last 2 turns), bootstrap from terminal
                float bootstrap = (t + 2 < T)
                    ? _positions[t + 2].NetPrediction
                    : terminal;

                float label = decay * terminal + (1f - decay) * bootstrap;
                result.Add((features, label));
            }

            return result;
        }
    }
}
