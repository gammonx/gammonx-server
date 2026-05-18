using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the distance the mother checker (the checker furthest back on the board) is away from HomeRange for the given player.
    /// </summary>
    public class MotherDistanceFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            // home entry is the first field of the home range in the direction of movement:
            // white moves toward HomeRangeWhite.Start (index 18), black toward HomeRangeBlack.Start (index 5)
            var homeEntry = isWhite
                ? board.HomeRangeWhite.Start.Value
                : board.HomeRangeBlack.Start.Value;

            var checkers = board.Fields.Index().Where(i => isWhite ? i.Item < 0 : i.Item > 0).ToList();
            if (board is IPinModel pinModel)
            {
                var pinned = pinModel.PinnedFields.Index().Where(i => isWhite ? i.Item < 0 : i.Item > 0);
                checkers = checkers.Concat(pinned).ToList();
            }

            if (checkers.Count == 0)
                return 0;

            var furthest = checkers.MaxBy(bi => board.RecoverRollOperator(isWhite, bi.Index, homeEntry));
            if (furthest == default)
                return 0;

            // clamp to 0 when the mother is already inside the home range
            return Math.Max(0, board.RecoverRollOperator(isWhite, furthest.Index, homeEntry));
        }
    }
}
