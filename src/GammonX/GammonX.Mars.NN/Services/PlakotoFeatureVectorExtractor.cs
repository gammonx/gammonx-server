using GammonX.Engine.History;
using GammonX.Engine.Models;

using GammonX.Engine.Services;

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
            // we always extract the feature from the perspective of the white player, so we invert the board if the active player is black
            var boardClone = isWhite ? board.DeepClone() : board.InvertBoard();

            var pinModel = (IPinModel)boardClone;
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
                0f,
                boardClone.BearOffCountWhite / 15f,
                boardClone.BearOffCountBlack / 15f,
                turnNumber / 100f,
            ];

            // we add the raw board as input
            var fields = boardClone.Fields;
            for (var index = 0; index < boardClone.Fields.Length; index++)
            {
                // white player view: own = positive, opponent = negative
                var v = fields[index];
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
            var pinnedFields = pinModel.PinnedFields;
            for (var index = 0; index < pinModel.PinnedFields.Length; index++)
            {
                // white player view: own = positive, opponent = negative
                var v = pinnedFields[index];
                features.Add(v >= 1 ? 1f : 0f);  // black pinned
                features.Add(v <= -1 ? 1f : 0f); // white pinned
            }

            return features.ToArray();
        }
    }
}
