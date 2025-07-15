
using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Helper class for checking and validating a specific board state or move.
    /// </summary>
    public static class BoardBroker
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

            // TODO :: implement and test IPinModel

            // we determine the final block amount
            var blockAmount = model.BlockAmount;
            if (model is IPinModel pinModel)
            {
                // we decrease the block amount if an opponents checker is pinned
                blockAmount -= Math.Abs(pinModel.PinnedFields[to]);
            }

            // check if the to point is blocked
            if (isWhite)
            {                

                // >= 2 black pieces on the to point
                if (toPoint >= blockAmount) return false;
            }
            else
            {
                // >= 2 white pieces on the to point
                if (toPoint <= -blockAmount) return false;
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
            if (!AllPiecesInHomeRange(model, isWhite)) 
                return false;

            // can be beared off if the roll moves the exactly on or beyond the home range
            return model.CanBearOffOperator(isWhite, from, roll);
        }

        /// <summary>
        /// Checks if a player has all pieces within the home range.
        /// </summary>
        /// <param name="model">Board to operate on.</param>
        /// <param name="isWhite">Indicates if white or black pieces are relevant.</param>
        /// <returns>Boolean indicating if all pieces are within the home range</returns>
        public static bool AllPiecesInHomeRange(this IBoardModel model, bool isWhite)
        {
            var homeRange = isWhite ? model.HomeRangeWhite : model.HomeRangeBlack;
            for (int i = 0; i < model.Fields.Length; i++)
            {
                int point = model.Fields[i];
                if (!model.IsInHomeOperator(isWhite, i))
                {
                    if ((isWhite && point < 0) || (!isWhite && point > 0))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
