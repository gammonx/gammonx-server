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
            if (isWhite)
            {
                var whitePositions = board.Fields.Index().Where(i => i.Item < 0).ToList();
                if (whitePositions.Count != 0)
                {
                    return Math.Abs(whitePositions.Average(wp => wp.Item));
                }
            }
            else
            {
                var blackPositions = board.Fields.Index().Where(i => i.Item > 0).ToList();
                if (blackPositions.Count != 0)
                {
                    return blackPositions.Average(bp => bp.Item);
                }
            }

            return 0;
        }
    }
}
