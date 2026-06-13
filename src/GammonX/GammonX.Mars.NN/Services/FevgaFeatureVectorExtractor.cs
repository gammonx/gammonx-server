using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

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
            // we always extract the feature from the perspective of the white player, so we invert the board if the active player is black
            var boardClone = isWhite ? board.DeepClone() : board.InvertBoard();

            var turnNumber = boardClone.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            List<float> features =
            [
                // self-crafted structural features
                isWhite ? (float)model.MaxPrimeLengthPlayer : (float)model.MaxPrimeLengthOpp,
                isWhite ? (float)model.MaxPrimeLengthOpp : (float)model.MaxPrimeLengthPlayer,
                isWhite ? (float)model.HomebarCountPlayer : (float)model.HomebarCountOpp,
                isWhite ? (float)model.HomebarCountOpp : (float)model.HomebarCountPlayer,
                isWhite ? (float)model.BlotCount : (float)model.BlotCountOpp, 
                isWhite ? (float)model.BlotCountOpp : (float)model.BlotCount,
                isWhite ? (float)model.AnchorCountInFrontPlayer : (float)model.AnchorCountInFrontOpp,
                isWhite ? (float)model.AnchorCountInFrontOpp : (float)model.AnchorCountInFrontPlayer,
                isWhite ? (float)model.AverageStackHeightPlayer : (float)model.AverageStackHeightOpp,
                isWhite ? (float)model.AverageStackHeightOpp : (float)model.AverageStackHeightPlayer,
                isWhite ? (float)model.AverageDistanceToBearOffPlayer : (float)model.AverageDistanceToBearOffOpp,
                isWhite ? (float)model.AverageDistanceToBearOffOpp : (float)model.AverageDistanceToBearOffPlayer,
                isWhite ? (float)model.AverageGapSizePlayer : (float)model.AverageGapSizeOpp,
                isWhite ? (float)model.AverageGapSizeOpp : (float)model.AverageGapSizePlayer,
                isWhite ? (float)model.CheckersInPrimeZonePlayer : (float)model.CheckersInPrimeZoneOpp,
                isWhite ? (float)model.CheckersInPrimeZoneOpp : (float)model.CheckersInPrimeZonePlayer,
                // race features
                isWhite ? (float)model.PipToBearOff : (float)model.PipToBearOffOpp,
                isWhite ? (float)model.PipToBearOffOpp : (float)model.PipToBearOff,
                isWhite ? (float)model.PipDifference : -(float)model.PipDifference,
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
            for (var i = 0; i < boardClone.Fields.Length; i++)
            {
                // white player view: own = positive, opponent = negative
                var v = fields[i];
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
