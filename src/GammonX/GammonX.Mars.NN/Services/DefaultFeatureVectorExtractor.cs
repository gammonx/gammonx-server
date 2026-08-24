using GammonX.Engine.History;
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
            var turnNumber = 0;
            foreach (var historyEvent in board.History.Events)
            {
                if (historyEvent.Type == HistoryEventType.Roll)
                {
                    turnNumber++;
                }
            }
            var features = new float[FeatureCount];
            var featureIndex = 0;

            // we always extract the feature vectors from whites perspective
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
            features[featureIndex++] = turnNumber / 100f;

            // we add the raw board as input
            var fields = board.Fields;
            for (var i = 0; i < fields.Length; i++)
            {
                // white players view are positive values, opponent displayed as negative values
                // TODO: verify this
                var v = isWhite ? fields[i] : -fields[fields.Length - 1 - i];
                features[featureIndex++] = v >= 1 ? 1f : 0f;  // own blot
                features[featureIndex++] = v >= 2 ? 1f : 0f;  // own anchor
                features[featureIndex++] = v >= 3 ? 1f : 0f;  // own 3+
                features[featureIndex++] = v >= 4 ? 1f : 0f;  // own 4+
                features[featureIndex++] = v <= -1 ? 1f : 0f; // opp blot
                features[featureIndex++] = v <= -2 ? 1f : 0f; // opp anchor
                features[featureIndex++] = v <= -3 ? 1f : 0f; // opp 3+
                features[featureIndex++] = v <= -4 ? 1f : 0f; // opp 4+
            }

            return features;
        }
    }
}
