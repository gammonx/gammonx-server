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
            var positions = new int[board.Fields.Length];
            var positionCount = 0;

            for (var index = 0; index < board.Fields.Length; index++)
            {
                var field = board.Fields[index];
                if ((isWhite && field < 0) || (!isWhite && field > 0))
                {
                    positions[positionCount++] = board.RecoverRollOperator(isWhite, startIndex, index);
                }
            }

            Array.Sort(positions, 0, positionCount);
            return CalculateAverageGap(positions, positionCount);
        }

        /// <summary>
        /// Returns the average number of empty points between consecutive checker positions
        /// when ordered by movement direction. Returns 0 when fewer than 2 checkers exist.
        /// </summary>
        private static double CalculateAverageGap(int[] sortedMovementPositions, int positionCount)
        {
            if (positionCount <= 1)
                return 0.0;

            double totalGap = 0;
            for (var i = 1; i < positionCount; i++)
            {
                totalGap += sortedMovementPositions[i] - sortedMovementPositions[i - 1] - 1;
            }

            return totalGap / (positionCount - 1);
        }
    }
}
