using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the average gap size between the players formation.
    /// Large gaps > impossible to form a prime soon
    /// Small gaps > high prime potential
    /// </summary>
    public class AverageGapSizeFeature : IFeature<double>
    {
        // <inheritdoc />
        public double Eval(IBoardModel board, bool isWhite)
        {
            var startIndex = isWhite
                ? board.StartRangeWhite.Start.Value
                : board.StartRangeBlack.Start.Value;

            var positions = board.Fields.Index()
                .Where(i => isWhite ? i.Item < 0 : i.Item > 0)
                .Select(i => board.RecoverRollOperator(isWhite, startIndex, i.Index))
                .Order()
                .ToList();

            return CalculateAverageGap(positions);
        }

        /// <summary>
        /// Returns the average number of empty points between consecutive checker positions
        /// when ordered by movement direction. Returns 0 when fewer than 2 checkers exist.
        /// </summary>
        private static double CalculateAverageGap(List<int> sortedMovementPositions)
        {
            if (sortedMovementPositions.Count <= 1)
                return 0.0;

            double totalGap = 0;
            for (int i = 1; i < sortedMovementPositions.Count; i++)
            {
                totalGap += sortedMovementPositions[i] - sortedMovementPositions[i - 1] - 1;
            }

            return totalGap / (sortedMovementPositions.Count - 1);
        }
    }
}
