using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public class FevgaFeatureVectorExtractor : IFeatureVectorExtractor
    {
        // <inheritdoc />
        public int FeatureCount => 21;

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
                (float)-model.BlotCount, // we need to invert the count in order to get the normalized value correct
                (float)model.BlotCountOpp,
                (float)model.AnchorCountInFrontPlayer,
                (float)model.AnchorCountInFrontOpp,
                (float)model.AverageStackHeightPlayer,
                (float)model.AverageStackHeightOpp,
                (float)model.AverageDistanceToBearOffPlayer,
                (float)model.AverageDistanceToBearOffOpp,
                (float)model.AverageGapSizePlayer,
                (float)model.AverageGapSizeOpp,
                (float)model.CheckersInPrimeZonePlayer,
                (float)model.CheckersInPrimeZoneOpp,
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
