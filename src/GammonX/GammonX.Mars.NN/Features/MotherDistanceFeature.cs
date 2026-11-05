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

            var distance = 0;
            for (var index = 0; index < board.Fields.Length; index++)
            {
                if (FeatureBoardGeometry.RemainingCountAt(board, isWhite, index) > 0)
                {
                    distance = Math.Max(distance, board.RecoverRollOperator(isWhite, index, homeEntry));
                }
            }
            return distance;
        }
    }
}
