using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts the average stack height for a given player.
    /// Should detect inefficient play, too many stacked checkers implies bad structure
    /// </summary>
    /// <remarks>
    /// Does not account for checker count on the homebar.
    /// </remarks>
    public class AverageStackHeightFeature : IFeature<double>
    {
        // <inheritdoc />
        public double Eval(IBoardModel board, bool isWhite)
        {
            var totalStackHeight = 0;
            var occupiedPointCount = 0;
            foreach (var field in board.Fields)
            {
                if ((isWhite && field < 0) || (!isWhite && field > 0))
                {
                    totalStackHeight += Math.Abs(field);
                    occupiedPointCount++;
                }
            }

            return occupiedPointCount > 0
                ? (double)totalStackHeight / occupiedPointCount
                : 0;
        }
    }
}
