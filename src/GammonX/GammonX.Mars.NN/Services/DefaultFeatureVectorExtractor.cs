using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides feature vector extractor capabilities for <see cref="GameModus.Backgammon"/>, <see cref="GameModus.Tavla"/>
    /// and <see cref="GameModus.Portes"/>.
    /// </summary>
    /// <seealso cref="IFeatureVectorExtractor"/>
    public class DefaultFeatureVectorExtractor : IFeatureVectorExtractor
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
            // TODO: we need scalar values between 0 and 1 for proper back propagation.
            if (features.Length != FeatureCount)
            {
                throw new ArgumentException("Incorrect feature buffer length.", nameof(features));
            }
            
            var featureIndex = 0;

            // we always extract the feature vectors from active players perspective
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
            features[featureIndex++] = (float)model.PipToBearOff;
            features[featureIndex++] = (float)model.PipToBearOffOpp;
            features[featureIndex++] = (float)model.PipDifference;
            features[featureIndex++] = model.Race ? 1f : 0f;
            features[featureIndex++] = 0f;
            features[featureIndex++] = (isWhite ? board.BearOffCountWhite : board.BearOffCountBlack) / 15f;
            features[featureIndex++] = (isWhite ? board.BearOffCountBlack : board.BearOffCountWhite) / 15f;
            features[featureIndex++] = (float)model.TurnNumber;

            // we add the raw board as input
            var fields = board.Fields;
            for (var i = 0; i < fields.Length; i++)
            {
                // we invert the board, so that the perspective always reflects the active player
                // white i=0 takes board.Fields[0] and black i=0 takes board.Fields[23]
                // the active player's checkers are positive; the opponent's checkers are negative
                var v = isWhite ? fields[i] : -fields[fields.Length - 1 - i];
                features[featureIndex++] = v >= 1 ? 1f : 0f;                // own blot
                features[featureIndex++] = v >= 2 ? 1f : 0f;                // own anchor
                features[featureIndex++] = v >= 3 ? 1f : 0f;                // own 3+
                // the count tail increases from 1/12 at four checkers to 1 at fifteen
                features[featureIndex++] = v >= 4 ? (v - 3) / 12f : 0f;     // own 4+
                features[featureIndex++] = v <= -1 ? 1f : 0f;               // opp blot
                features[featureIndex++] = v <= -2 ? 1f : 0f;               // opp anchor
                features[featureIndex++] = v <= -3 ? 1f : 0f;               // opp 3+
                // the count tail increases from 1/12 at four checkers to 1 at fifteen
                features[featureIndex++] = v <= -4 ? Math.Abs(v + 3) / 12f : 0f; // opp 4+
            }


        }
    }
}
