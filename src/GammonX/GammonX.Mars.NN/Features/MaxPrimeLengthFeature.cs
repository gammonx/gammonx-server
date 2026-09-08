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
            var pinModel = board as IPinModel;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                var field = board.Fields[index];
                var isAnchor = isWhite
                    ? field <= -board.BlockAmount
                    : field >= board.BlockAmount;
                if (!isAnchor && pinModel is not null)
                {
                    isAnchor = isWhite
                        ? field == -(board.BlockAmount - 1) && pinModel.PinnedFields[index] == board.BlockAmount - 1
                        : field == board.BlockAmount - 1 && pinModel.PinnedFields[index] == -(board.BlockAmount - 1);
                }

                if (isAnchor)
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
