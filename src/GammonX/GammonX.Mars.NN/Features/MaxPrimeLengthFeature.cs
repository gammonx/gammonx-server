using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the length of the longest prime of the given player
    /// </summary>
    public class MaxPrimeLengthFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            var longest = 0;
            var current = 0;
            for (var position = 0; position < board.Fields.Length; position++)
            {
                var index = FeatureBoardGeometry.BoardIndex(board, isWhite, position);
                if (FeatureBoardGeometry.IsMadePoint(board, isWhite, index))
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else
                {
                    current = 0;
                }
            }

            return longest;
        }
    }
}
