using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server.Services
{
    // <inheritdoc />
    public class FevgaFeatureVectorExtractor : IFeatureVectorExtractor
    {
        private const int _featureCount = 10;

        // <inheritdoc />
        public int FeatureCount => _featureCount;

        // <inheritdoc />
        public float[] Extract(NormalizedEvalResultModel model)
        {
            return new float[_featureCount]
            {
                // contact features
                (float)model.PrimeProbabilityPlayer,
                (float)model.PrimeProbabilityOpp,
                // structure
                (float)model.MaxPrimeLengthPlayer,
                (float)model.MaxPrimeLengthOpp,
                (float)model.HomebarCountPlayer,
                (float)-model.BlotCount,
                // race
                (float)model.PipToBearOff,
                (float)model.PipToBearOffOpp,
                (float)model.PipDifference,
                // race flag
                model.Race ? 1f : 0f,
            };
        }
    }
}
