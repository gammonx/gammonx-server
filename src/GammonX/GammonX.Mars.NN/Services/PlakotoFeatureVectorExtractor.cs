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
                // contact features
                //(float)model.HitProbability1,
                //(float)model.HitProbability2,
                //(float)model.HitOpponentProbability1,
                //(float)model.HitOpponentProbability2,
                //(float)model.EscapeProbability1,
                //(float)model.EscapeProbability2,
                //(float)model.EscapeProbability1Opp,
                //(float)model.EscapeProbability2Opp,
                // pin features
                (float)model.PinCountOpp,
                (float)model.PinCountPlayer,
                (float)model.OppMotherPinned,
                (float)model.PlayerMotherPinned,
                (float)model.MotherDistancePlayer,
                (float)model.MotherDistanceOpp,
                (float)model.NumChFrontLastPin,
                (float)model.NumChFrontLastPinOpp,
                // structure
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
                // race
                (float)model.PipDifference,
                (float)model.PipToBearOff,
                (float)model.PipToBearOffOpp,
                // race flag
                model.Race ? 1f : 0f,
                // raw board tensors
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
