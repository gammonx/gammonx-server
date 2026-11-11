using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts visible player checkers strictly behind the opponent pin nearest
    /// bearing off in the player's home board. Returns zero without a home pin.
    /// </summary>
    public sealed class NumbersOfCheckersInFrontOfLastPinFeature : IFeature<int>
    {
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (board is not IPinModel pins)
                return 0;

            var lastPinPosition = -1;
            for (var index = 0; index < pins.PinnedFields.Length; index++)
            {
                if (FeatureBoardGeometry.OwnCount(pins.PinnedFields[index], !isWhite) > 0
                    && board.IsInHomeOperator(isWhite, index))
                {
                    lastPinPosition = Math.Max(lastPinPosition, FeatureBoardGeometry.MovementPosition(board, isWhite, index));
                }
            }

            var count = 0;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                var own = FeatureBoardGeometry.OwnCount(board.Fields[index], isWhite);
                if (own > 0 && FeatureBoardGeometry.MovementPosition(board, isWhite, index) < lastPinPosition)
                {
                    count += own;
                }
            }
            return count;
        }
    }
}
