using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts player made points ahead of the opponent's rearmost remaining
    /// checker, including pinned opponents and opponents on bar/reserve.
    /// </summary>
    public class AnchorCountInFrontFeature : IFeature<int>
    {
        public int Eval(IBoardModel board, bool isWhite)
        {
            var opponentRearDistance = FeatureBoardGeometry.RearDistance(board, !isWhite);
            var count = 0;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                if (FeatureBoardGeometry.IsMadePoint(board, isWhite, index)
                    && FeatureBoardGeometry.BearOffDistance(board, !isWhite, index) < opponentRearDistance)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
