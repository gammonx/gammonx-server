using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates mean exact bear-off distance over remaining visible, pinned, and
    /// bar/reserve checkers. Borne-off checkers are excluded from the denominator.
    /// </summary>
    public class AverageDistanceToBearOffFeature : IFeature<double>
    {
        // <inheritdoc />
        public double Eval(IBoardModel board, bool isWhite)
        {
            var totalCheckers = FeatureBoardGeometry.BarCount(board, isWhite);
            var totalDistance = totalCheckers * (board.Fields.Length + 1);
            for (var index = 0; index < board.Fields.Length; index++)
            {
                var count = FeatureBoardGeometry.RemainingCountAt(board, isWhite, index);
                totalCheckers += count;
                if (count > 0)
                    totalDistance += count * FeatureBoardGeometry.BearOffDistance(board, isWhite, index);
            }
            return totalCheckers > 0 ? (double)totalDistance / totalCheckers : 0.0;
        }
    }
}
