using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;

namespace GammonX.Mars.Training
{
    /// <summary>
    /// Records (features, outcome) pairs during self-play for NN training.
    /// Uses TD-lambda discounting so that early positions receive a label closer to 0.5
    /// (uncertain) and later positions receive a label closer to the terminal result (0 or 1).
    /// </summary>
    public sealed class SelfPlayRecorder
    {
        /// <summary>
        /// Default lambda for TD-lambda discounting.
        /// Values closer to 1.0 assign nearly equal weight to all positions.
        /// Values closer to 0.0 concentrate the signal on the final positions.
        /// </summary>
        public const float DefaultLambda = 0.95f;

        private readonly IFeatureVectorExtractor _featureVectorExtractor;
        private readonly float _lambda;

        // features and the isWhite flag are stored together so Finalize can assign
        // the correct label per position without needing two separate recorders.
        private readonly List<(float[] Features, bool IsWhite)> _positions = [];

        public SelfPlayRecorder(IFeatureVectorExtractor featureVectorExtractor, float lambda = DefaultLambda)
        {
            if (lambda is < 0f or > 1f)
                throw new ArgumentOutOfRangeException(nameof(lambda), "Lambda must be in [0, 1].");

            _featureVectorExtractor = featureVectorExtractor;
            _lambda = lambda;
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
            _positions.Add((_featureVectorExtractor.Extract(eval), isWhite));
        }

        /// <summary>
        /// Finalizes the recording and returns all (features, label) training samples.
        /// Labels are discounted with TD-lambda: label[t] = λ^(T-1-t) * terminal + (1 - λ^(T-1-t)) * 0.5
        /// where T is the total number of recorded positions and t is the position index (0-based).
        /// The last position always receives the exact terminal label.
        /// </summary>
        /// <param name="whiteWon"><c>true</c> if white won the game.</param>
        public IReadOnlyList<(float[] Features, float Label)> Finalize(bool whiteWon)
        {
            int T = _positions.Count;
            var result = new (float[] Features, float Label)[T];

            for (int t = 0; t < T; t++)
            {
                var (features, isWhite) = _positions[t];
                var terminalLabel = (isWhite == whiteWon) ? 1.0f : 0.0f;

                int stepsFromEnd = T - 1 - t;
                float decay = MathF.Pow(_lambda, stepsFromEnd);
                float label = decay * terminalLabel + (1f - decay) * 0.5f;
                result[t] = (features, label);
            }

            return result;
        }
    }
}
