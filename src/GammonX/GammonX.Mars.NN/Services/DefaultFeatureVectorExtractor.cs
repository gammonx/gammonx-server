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
            // we always extract the feature vectors from whites perspective
            var boardCopy = isWhite ? board : board.InvertBoard();

            var turnNumber = board.History.Events.Count(e => e.Type == HistoryEventType.Roll);

            List<float> features =
            [
                // self-crafted structural features
                (float)model.MaxPrimeLengthPlayer,
                (float)model.MaxPrimeLengthOpp,
                (float)model.HomebarCountPlayer,
                (float)model.HomebarCountOpp,
                (float)model.BlotCount,
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
                0f,
                boardCopy.BearOffCountWhite / 15f,
                boardCopy.BearOffCountBlack / 15f,
                turnNumber / 100f,
            ];

            // we add the raw board as input
            var fields = boardCopy.Fields;
            for (var i = 0; i < boardCopy.Fields.Length; i++)
            {
                // white players view are positive values, opponent displayed as negative values
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
