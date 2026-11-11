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
            for (var index = 0; index < board.Fields.Length; index++)
            {
                if (FeatureBoardGeometry.IsBlot(board, isWhite, index))
                {
                    blotCount++;
                }
            }

            return blotCount;
        }
    }
}
