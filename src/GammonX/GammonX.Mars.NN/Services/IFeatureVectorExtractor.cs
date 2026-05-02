using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    public interface IFeatureVectorExtractor
    {
        /// <summary>
        /// Gets the amount of features vectors extracted by this extractor.
        /// </summary>
        /// <remarks></remarks>
        public int FeatureCount { get; }

        /// <summary>
        /// Extracts the normalized feature vector from an eval result for use as NN input.
        /// All values are already in [0, 1] via <see cref="NormalizedEvalResultModel"/>.
        /// </summary>
        /// <returns>A normalized feature vector as a float array.</returns>
        float[] Extract(NormalizedEvalResultModel n);
    }

    public static class FeatureVectorExtractorFactory
    {
        public static IFeatureVectorExtractor Create(GameModus modus)
        {
            return modus switch
            {
                GameModus.Plakoto => new PlakotoFeatureVectorExtractor(),
                GameModus.Fevga => new FevgaFeatureVectorExtractor(),
                _ => throw new NotSupportedException($"Modus {modus} has no feature vector extractor.")
            };
        }
    }
}
