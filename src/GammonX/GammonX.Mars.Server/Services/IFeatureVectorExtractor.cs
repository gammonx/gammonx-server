using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server.Services
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
}
