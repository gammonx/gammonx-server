using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates how many blots the player has on the board.
    /// </summary>
    public class BlotCountFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            var blotCount = 0;
            var pinModel = board as IPinModel;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                var isBlot = isWhite
                    ? board.Fields[index] == -1
                    : board.Fields[index] == 1;
                if (isBlot && (pinModel is null || pinModel.PinnedFields[index] == 0))
                {
                    blotCount++;
                }
            }

            return blotCount;
        }
    }
}
