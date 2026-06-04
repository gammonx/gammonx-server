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
            var turnNumber = boardClone.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            List<float> features =
            [
                // self-crafted pin features
                isWhite ? (float)model.PinCountOpp : (float)model.PinCountPlayer,
                isWhite ? (float)model.PinCountPlayer : (float)model.PinCountOpp,
                isWhite ? (float)model.OppMotherPinned : (float)model.PlayerMotherPinned,
                isWhite ? (float)model.PlayerMotherPinned : (float)model.OppMotherPinned,
                isWhite ? (float)model.MotherDistancePlayer : (float)model.MotherDistanceOpp,
                isWhite ? (float)model.MotherDistanceOpp : (float)model.MotherDistancePlayer,
                isWhite ? (float)model.NumChFrontLastPin : (float)model.NumChFrontLastPinOpp,
                isWhite ? (float)model.NumChFrontLastPinOpp : (float)model.NumChFrontLastPin,
                // self-crafted structural features
                isWhite ? (float)model.BlotCount : (float)model.BlotCountOpp,
                isWhite ? (float)model.BlotCountOpp : (float)model.BlotCount,
                isWhite ? (float)model.BlotInStartRangeCount : (float)model.BlotInStartRangeCountOpp,
                isWhite ? (float)model.BlotInStartRangeCountOpp : (float)model.BlotInStartRangeCount,
                isWhite ? (float)model.AnchorCount : (float)model.AnchorCountOpp,
                isWhite ? (float)model.AnchorCountOpp : (float)model.AnchorCount,
                isWhite ? (float)model.AverageStackHeightPlayer : (float)model.AverageStackHeightOpp,
                isWhite ? (float)model.AverageStackHeightOpp : (float)model.AverageStackHeightPlayer,
                isWhite ? (float)model.AverageDistanceToBearOffPlayer : (float)model.AverageDistanceToBearOffOpp,
                isWhite ? (float)model.AverageDistanceToBearOffOpp : (float)model.AverageDistanceToBearOffPlayer,
                // race features
                isWhite ? (float)model.PipDifference : -(float)model.PipDifference,
                isWhite ? (float)model.PipToBearOff : (float)model.PipToBearOffOpp,
                isWhite ? (float)model.PipToBearOffOpp : (float)model.PipToBearOff,
                // race feature flag
                model.Race ? 1f : 0f,
                // raw board feature tensors
                isWhite ? 1f : 0f,
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
