using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;

namespace GammonX.Mars.Training
{
    /// <summary>
    /// Records (features, outcome) pairs during self-play for NN training.
    /// Each position is labelled with the terminal game outcome from the active
    /// player's perspective: 1.0 = that player won, 0.0 = that player lost.
    /// </summary>
    public sealed class SelfPlayRecorder
    {
        public const float DefaultLambda = 0.95f; // TODO

        private readonly IFeatureVectorExtractor _featureVectorExtractor;
        private readonly List<(float[] Features, bool IsWhite)> _positions = [];

        public SelfPlayRecorder(IFeatureVectorExtractor featureVectorExtractor, float lambda = DefaultLambda)
        {
            _featureVectorExtractor = featureVectorExtractor;
            // TODO: lambda reserved for future TD(λ) bootstrapping once base model converges
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
        /// Each position receives the terminal game outcome from the active player's perspective:
        /// 1.0 if that player won, 0.0 if they lost.
        /// </summary>
        /// <param name="whiteWon"><c>true</c> if white won the game.</param>
        public IReadOnlyList<(float[] Features, float Label)> Finalize(bool whiteWon)
        {
            int T = _positions.Count;
            var result = new (float[] Features, float Label)[T];

            for (int t = 0; t < T; t++)
            {
                var (features, isWhite) = _positions[t];
                var label = (isWhite == whiteWon) ? 1.0f : 0.0f;
                result[t] = (features, label);
            }

            return result;
        }
    }
}
