using GammonX.Engine.History;
using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public class PlakotoFeatureVectorExtractor : IFeatureVectorExtractor
    {
        // <inheritdoc />
        public int FeatureCount => 266;

        // <inheritdoc />
        public float[] Extract(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            var pinModel = (IPinModel)board;
            var turnNumber = board.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            List<float> features =
            [
                // self-crafted pin features
                (float)model.PinCountOpp,
                (float)model.PinCountPlayer,
                (float)model.OppMotherPinned,
                (float)model.PlayerMotherPinned,
                (float)model.MotherDistancePlayer,
                (float)model.MotherDistanceOpp,
                (float)model.NumChFrontLastPin,
                (float)model.NumChFrontLastPinOpp,
                // self-crafted structural features
                (float)model.BlotCount,
                (float)model.BlotCountOpp,
                (float)model.BlotInStartRangeCount,
                (float)model.BlotInStartRangeCountOpp,
                (float)model.AnchorCount,
                (float)model.AnchorCountOpp,
                (float)model.AverageStackHeightPlayer,
                (float)model.AverageStackHeightOpp,
                (float)model.AverageDistanceToBearOffPlayer,
                (float)model.AverageDistanceToBearOffOpp,
                // race features
                (float)model.PipDifference,
                (float)model.PipToBearOff,
                (float)model.PipToBearOffOpp,
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
            for (var index = 0; index < board.Fields.Length; index++)
            {
                // active player view: own = positive
                var v = fields[index] * sign;
                features.Add(v >= 1 ? 1f : 0f);  // own blot
                features.Add(v >= 2 ? 1f : 0f);  // own anchor
                features.Add(v >= 3 ? 1f : 0f);  // own 3+
                features.Add(v >= 4 ? 1f : 0f);  // own 4+
                features.Add(v <= -1 ? 1f : 0f); // opp blot
                features.Add(v <= -2 ? 1f : 0f); // opp anchor
                features.Add(v <= -3 ? 1f : 0f); // opp 3+
                features.Add(v <= -4 ? 1f : 0f); // opp 4+
            }
            // we add the raw pinned fields as input
            var pinnedFields = isWhite ? pinModel.PinnedFields : pinModel.PinnedFields.Reverse().ToArray();
            for (var index = 0; index < pinModel.PinnedFields.Length; index++)
            {
                // active player view: own = positive
                var v = pinnedFields[index] * sign;
                features.Add(v >= 1 ? 1f : 0f);  // black pinned
                features.Add(v <= -1 ? 1f : 0f); // white pinned
            }

            return features.ToArray();
        }
    }
}
