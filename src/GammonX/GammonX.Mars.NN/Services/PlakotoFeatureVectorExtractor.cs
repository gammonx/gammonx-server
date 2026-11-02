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
            var features = new float[FeatureCount];
            Extract(model, board, isWhite, features);
            return features;
        }

        // <inheritdoc />
        public void Extract(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, Span<float> features)
        {
            if (features.Length != FeatureCount)
            {
                throw new ArgumentException("Incorrect feature buffer length.", nameof(features));
            }
            
            // we always extract the feature from the perspective of the white player, so we invert the board if the active player is black
            var boardClone = isWhite ? board.DeepClone() : board.InvertBoard();

            var pinModel = (IPinModel)boardClone;
            var turnNumber = board.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            var featureIndex = 0;
            // self-crafted pin features
            features[featureIndex++] = (float)model.PinCountOpp;
            features[featureIndex++] = (float)model.PinCountPlayer;
            features[featureIndex++] = (float)model.OppMotherPinned;
            features[featureIndex++] = (float)model.PlayerMotherPinned;
            features[featureIndex++] = (float)model.MotherDistancePlayer;
            features[featureIndex++] = (float)model.MotherDistanceOpp;
            features[featureIndex++] = (float)model.NumChFrontLastPin;
            features[featureIndex++] = (float)model.NumChFrontLastPinOpp;
            // self-crafted structural features
            features[featureIndex++] = (float)model.BlotCount;
            features[featureIndex++] = (float)model.BlotCountOpp;
            features[featureIndex++] = (float)model.BlotInStartRangeCount;
            features[featureIndex++] = (float)model.BlotInStartRangeCountOpp;
            features[featureIndex++] = (float)model.AnchorCount;
            features[featureIndex++] = (float)model.AnchorCountOpp;
            features[featureIndex++] = (float)model.AverageStackHeightPlayer;
            features[featureIndex++] = (float)model.AverageStackHeightOpp;
            features[featureIndex++] = (float)model.AverageDistanceToBearOffPlayer;
            features[featureIndex++] = (float)model.AverageDistanceToBearOffOpp;
            // race features
            features[featureIndex++] = (float)model.PipDifference;
            features[featureIndex++] = (float)model.PipToBearOff;
            features[featureIndex++] = (float)model.PipToBearOffOpp;
            // race feature flag
            features[featureIndex++] = model.Race ? 1f : 0f;
            // raw board feature tensors
            features[featureIndex++] = 0f;
            features[featureIndex++] = boardClone.BearOffCountWhite / 15f;
            features[featureIndex++] = boardClone.BearOffCountBlack / 15f;
            features[featureIndex++] = turnNumber / 100f;

            // we add the raw board as input
            var fields = boardClone.Fields;
            for (var index = 0; index < boardClone.Fields.Length; index++)
            {
                // white player view: own = positive, opponent = negative
                var v = fields[index];
                features[featureIndex++] = v >= 1 ? 1f : 0f;  // own blot
                features[featureIndex++] = v >= 2 ? 1f : 0f;  // own anchor
                features[featureIndex++] = v >= 3 ? 1f : 0f;  // own 3+
                features[featureIndex++] = v >= 4 ? 1f : 0f;  // own 4+
                features[featureIndex++] = v <= -1 ? 1f : 0f; // opp blot
                features[featureIndex++] = v <= -2 ? 1f : 0f; // opp anchor
                features[featureIndex++] = v <= -3 ? 1f : 0f; // opp 3+
                features[featureIndex++] = v <= -4 ? 1f : 0f; // opp 4+
            }
            // we add the raw pinned fields as input
            var pinnedFields = pinModel.PinnedFields;
            for (var index = 0; index < pinModel.PinnedFields.Length; index++)
            {
                // white player view: own = positive, opponent = negative
                var v = pinnedFields[index];
                features[featureIndex++] = v >= 1 ? 1f : 0f;  // black pinned
                features[featureIndex++] = v <= -1 ? 1f : 0f; // white pinned
            }


        }
    }
}
