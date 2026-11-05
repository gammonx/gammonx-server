using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates how many blots the player has in his start range.
    /// </summary>
    public class BlotInStartRangeCountFeature
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            var count = 0;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                if (FeatureBoardGeometry.IsBlot(board, isWhite, index)
                    && board.IsInStartOperator(isWhite, index))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
