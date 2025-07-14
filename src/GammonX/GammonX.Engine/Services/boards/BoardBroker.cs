
using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Helper class for board operations
    /// </summary>
    internal static class BoardBroker
    {
        /// <summary>
        /// Checks if the given move can be made on the board.
        /// </summary>
        /// <remarks>
        /// Move direction is determined when the to position is calculated.
        /// Therefore, it is not necessary to check the direction of the move.
        /// </remarks>
        /// <param name="model">Board to check on.</param>
        /// <param name="from">From position index.</param>
        /// <param name="to">To position index.</param>
        /// <param name="isWhite">Indicates if white or black pieces are moved.</param>
        /// <returns>Boolean indicating if the move can be made.</returns>
        public static bool CanMove(IBoardModel model, int from, int to, bool isWhite)
        {
            // check indices validity
            if (from < 0 || from > 23 || to < 0 || to > 23)
                return false;

            int fromPoint = model.Points[from];
            int toPoint = model.Points[to];

            // check if a piece is available on the from point
            if (isWhite)
            {
                // no white pieces on from point
                if (fromPoint >= 0) return false;
            }
            else
            {
                // no black pieces on from point
                if (fromPoint <= 0) return false;
            }

            // check if the to point is blocked
            if (isWhite)
            {
                // >= 2 black pieces on the to point
                if (toPoint >= model.BlockAmount) return false;
            }
            else
            {
                // >= 2 white pieces on the to point
                if (toPoint <= -model.BlockAmount) return false;
            }

            return true;
        }
    }
}
