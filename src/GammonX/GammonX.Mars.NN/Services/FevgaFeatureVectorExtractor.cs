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

            // Canonicalize the board to white; model scalars already describe the active player.
            // White checkers stay negative. Preserve the opponent-first raw channel order.
            var boardClone = isWhite ? board.DeepClone() : board.InvertBoard();

            var featureIndex = 0;
            // self-crafted structural features
            features[featureIndex++] = (float)model.MaxPrimeLengthPlayer;
            features[featureIndex++] = (float)model.MaxPrimeLengthOpp;
            features[featureIndex++] = (float)model.HomebarCountPlayer;
            features[featureIndex++] = (float)model.HomebarCountOpp;
            features[featureIndex++] = (float)model.BlotCount;
            features[featureIndex++] = (float)model.BlotCountOpp;
            features[featureIndex++] = (float)model.AnchorCountInFrontPlayer;
            features[featureIndex++] = (float)model.AnchorCountInFrontOpp;
            features[featureIndex++] = (float)model.AverageStackHeightPlayer;
            features[featureIndex++] = (float)model.AverageStackHeightOpp;
            features[featureIndex++] = (float)model.AverageDistanceToBearOffPlayer;
            features[featureIndex++] = (float)model.AverageDistanceToBearOffOpp;
            features[featureIndex++] = (float)model.AverageGapSizePlayer;
            features[featureIndex++] = (float)model.AverageGapSizeOpp;
            features[featureIndex++] = (float)model.CheckersInPrimeZonePlayer;
            features[featureIndex++] = (float)model.CheckersInPrimeZoneOpp;
            // race features
            features[featureIndex++] = (float)model.PipToBearOff;
            features[featureIndex++] = (float)model.PipToBearOffOpp;
            features[featureIndex++] = (float)model.PipDifference;
            // race feature flag
            features[featureIndex++] = model.Race ? 1f : 0f;
            // raw board feature tensors
            features[featureIndex++] = 0f;
            features[featureIndex++] = boardClone.BearOffCountWhite / 15f;
            features[featureIndex++] = boardClone.BearOffCountBlack / 15f;
            features[featureIndex++] = (float)model.TurnNumber;

            // we add the raw board as input
            var fields = boardClone.Fields;
            for (var i = 0; i < boardClone.Fields.Length; i++)
            {
                // Canonical white view: own = negative, opponent = positive.
                var v = fields[i];
                features[featureIndex++] = v >= 1 ? 1f : 0f;                // opponent count >= 1
                features[featureIndex++] = v >= 2 ? 1f : 0f;                // opponent count >= 2
                features[featureIndex++] = v >= 3 ? 1f : 0f;                // opponent count >= 3
                // the count tail increases from 1/12 at four checkers to 1 at fifteen
                features[featureIndex++] = v >= 4 ? (v - 3) / 12f : 0f;     // opponent count tail
                features[featureIndex++] = v <= -1 ? 1f : 0f;               // player count >= 1
                features[featureIndex++] = v <= -2 ? 1f : 0f;               // player count >= 2
                features[featureIndex++] = v <= -3 ? 1f : 0f;               // player count >= 3
                // the count tail increases from 1/12 at four checkers to 1 at fifteen
                features[featureIndex++] = v <= -4 ? Math.Abs(v + 3) / 12f : 0f; // player count tail
            }


        }
    }
}
