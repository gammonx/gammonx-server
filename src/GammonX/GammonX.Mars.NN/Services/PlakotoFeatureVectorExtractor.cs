using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public class PlakotoFeatureVectorExtractor : IFeatureVectorExtractor
    {
        // <inheritdoc />
        public int FeatureCount => 21;

        // <inheritdoc />
        public float[] Extract(NormalizedEvalResultModel model)
        {
            return
            [
                // contact features
                (float)model.HitProbability1,
                (float)model.HitProbability2,
                (float)model.HitOpponentProbability1,
                (float)model.HitOpponentProbability2,
                (float)model.EscapeProbability1,
                (float)model.EscapeProbability2,
                (float)model.EscapeProbability1Opp,
                (float)model.EscapeProbability2Opp,
                // pin features
                (float)model.PinCountOpp,
                (float)model.PinCountPlayer,
                (float)model.OppMotherPinned,
                (float)model.PlayerMotherPinned,
                (float)model.NumChFrontLastPin,
                (float)model.NumChFrontLastPinOpp,
                // structure
                (float)model.BlotCount,
                (float)model.BlotInStartRangeCount,
                (float)model.AnchorCount,
                // race
                (float)model.PipDifference,
                (float)model.PipToBearOff,
                (float)model.PipToBearOffOpp,
                // race flag
                model.Race ? 1f : 0f
            ];
        }
    }
}
