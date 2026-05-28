using GammonX.Engine.History;
using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public class FevgaFeatureVectorExtractor : IFeatureVectorExtractor
    {
        // <inheritdoc />
        public int FeatureCount => 216;

        // <inheritdoc />
        public float[] Extract(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var turnNumber = board.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            List<float> features =
            [
                // self-crafted structural features
                (float)model.MaxPrimeLengthPlayer,
                (float)model.MaxPrimeLengthOpp,
                (float)model.HomebarCountPlayer,
                (float)model.HomebarCountOpp,
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
                // race features
                (float)model.PipToBearOff,
                (float)model.PipToBearOffOpp,
                (float)model.PipDifference,
                // race feature flag
                model.Race ? 1f : 0f,
                // raw board feature tensors
                isWhite ? 1f : 0f,
                board.BearOffCountWhite / 15f,
                board.BearOffCountBlack / 15f,
                turnNumber / 100f,
            ];

            // we add the raw board as input
            var sign = isWhite ? -1 : 1;
            var fields = isWhite ? board.Fields : board.Fields.Reverse().ToArray();
            for (var i = 0; i < board.Fields.Length; i++)
            {
                // active player view: own = positive
                var v = fields[i] * sign;
                features.Add(v >= 1 ? 1f : 0f);  // own blot
                features.Add(v >= 2 ? 1f : 0f);  // own anchor
                features.Add(v >= 3 ? 1f : 0f);  // own 3+
                features.Add(v >= 4 ? 1f : 0f);  // own 4+
                features.Add(v <= -1 ? 1f : 0f); // opp blot
                features.Add(v <= -2 ? 1f : 0f); // opp anchor
                features.Add(v <= -3 ? 1f : 0f); // opp 3+
                features.Add(v <= -4 ? 1f : 0f); // opp 4+
            }

            return features.ToArray();
        }
    }
}
