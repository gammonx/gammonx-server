
using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Helper class for board operations.
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
        public static bool CanMove(this IBoardModel model, int from, int to, bool isWhite)
        {
            // check indices validity
            if (from < 0 || from > 23 || to < 0 || to > 23)
                return false;

            int fromPoint = model.Fields[from];
            int toPoint = model.Fields[to];

            // check if a checker is available on the from point
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

        /// <summary>
        /// Checks if the given checker can be beared of based on the given roll.
        /// </summary>
        /// <param name="model">Board model to operate on.</param>
        /// <param name="from">The from position.</param>
        /// <param name="roll">The roll value.</param>
        /// <param name="isWhite">Indicates if white or black pieces are relevant.</param>
        /// <returns>Boolean indicating if the checker can be beared off.</returns>
        public static bool CanBearOff(this IBoardModel model, int from, int roll, bool isWhite)
        {
            // TODO: UNIT TESTS
            if (!AllPiecesInHomeRange(model, isWhite)) 
                return false;

            int to = isWhite ? model.WhiteMoveOperator(from, roll) : model.BlackMoveOperator(from, roll);

            // can be beared off if the roll moves the exactly on or beyond the home range
            if (isWhite)
            {
                return to < 0;
            }
            else
            {
                return to > 23;
            }
        }

        /// <summary>
        /// Checks if a player has all pieces within the home range.
        /// </summary>
        /// <param name="model">Board to operate on.</param>
        /// <param name="isWhite">Indicates if white or black pieces are relevant.</param>
        /// <returns>Boolean indicating if all pieces are within the home range</returns>
        public static bool AllPiecesInHomeRange(this IBoardModel model, bool isWhite)
        {
            // TODO: UNIT TESTS
            var homeRange = isWhite ? model.HomeRangeWhite : model.HomeRangeBlack;
            for (int i = 0; i < model.Fields.Length; i++)
            {
                int point = model.Fields[i];
                if (isWhite && point < 0 && (i < homeRange.Start.Value || i >= homeRange.End.Value)) return false;
                if (!isWhite && point > 0 && (i < homeRange.Start.Value || i >= homeRange.End.Value)) return false;
            }
            return true;
        }
    }
}
