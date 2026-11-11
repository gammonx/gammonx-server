using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// A race has no shared board point in the two sides' remaining movement paths.
    /// Includes hidden pinned checkers and entry paths from bar/reserve.
    /// </summary>
    public sealed class RaceFeature : IFeature<bool>
    {
        public bool Eval(IBoardModel board, bool isWhite) =>
            (RemainingPath(board, true) & RemainingPath(board, false)) == 0;

        private static uint RemainingPath(IBoardModel board, bool isWhite)
        {
            var rearDistance = FeatureBoardGeometry.RearDistance(board, isWhite);

            // Every other remaining checker follows a suffix of the rearmost checkers path.
            var firstPosition = Math.Max(board.Fields.Length - rearDistance, 0);
            uint path = 0;

            for (var position = firstPosition; position < board.Fields.Length; position++)
            {
                path |= 1u << FeatureBoardGeometry.BoardIndex(board, isWhite, position);
            }

            return path;
        }
    }
}
