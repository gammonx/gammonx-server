using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public class FevgaFeatureVectorExtractor : IFeatureVectorExtractor
    {
        // <inheritdoc />
        public int FeatureCount => 10;

        // <inheritdoc />
        public float[] Extract(NormalizedEvalResultModel model)
        {
            return
            [
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
                model.Race ? 1f : 0f
            ];
        }
    }
}
