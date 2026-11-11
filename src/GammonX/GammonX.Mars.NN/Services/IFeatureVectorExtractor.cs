using GammonX.Engine.Models;
using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides an interface for extracting normalized feature vectors from evaluation results and board states for use as input to neural networks.
    /// </summary>
    public interface IFeatureVectorExtractor
    {
        /// <summary>
        /// Gets the number of channels in each extracted feature vector.
        /// </summary>
        /// <remarks></remarks>
        public int FeatureCount { get; }

        /// <summary>
        /// Extracts the normalized feature vector from an eval result for use as NN input.
        /// Requires normalized model measurements and a valid board. Channels retain
        /// their fixed order: active-player scalars, then opponent-first raw point counts,
        /// followed by opponent/player pin indicators in Plakoto.
        /// </summary>
        /// <param name="model">Computed model values from the perspective of the active player.</param>
        /// <param name="board">Target board state from the perspective of the white player (not inverted).</param>
        /// <param name="isWhite">Player indicator.</param>
        /// <returns>A normalized feature vector as a float array.</returns>
        float[] Extract(NormalizedEvalResultModel model, IBoardModel board, bool isWhite);

        /// <summary>
        /// Writes every feature into caller-owned storage of exactly FeatureCount elements.
        /// </summary>
        /// <param name="model">Computed model values from the perspective of the active player.</param>
        /// <param name="board">Target board state from the perspective of the white player (not inverted).</param>
        /// <param name="isWhite">Player indicator.</param>
        /// <param name="destination">Caller-owned storage for the features. Must have exactly FeatureCount elements.</param>
        void Extract(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, Span<float> destination);
    }

    public static class FeatureVectorExtractorFactory
    {
        public static IFeatureVectorExtractor Create(GameModus modus)
        {
            return modus switch
            {
                GameModus.Plakoto => new PlakotoFeatureVectorExtractor(),
                GameModus.Fevga => new FevgaFeatureVectorExtractor(),
                GameModus.Backgammon => new DefaultFeatureVectorExtractor(),
                GameModus.Tavla => new DefaultFeatureVectorExtractor(),
                GameModus.Portes => new DefaultFeatureVectorExtractor(),
                _ => throw new NotSupportedException($"Modus {modus} has no feature vector extractor.")
            };
        }
    }
}
