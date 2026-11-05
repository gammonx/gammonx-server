using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    public class AnchorCountFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            var count = 0;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                if (FeatureBoardGeometry.IsMadePoint(board, isWhite, index))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
