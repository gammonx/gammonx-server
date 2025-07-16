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
        /// 1) Validates the max and min indices of the fields arrays.
        /// 2) Validates if a checker is available on the from position.
        /// 3) Validates if the to position is not blocked by the opponents checker.
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
            // if home bar model is implemented, some indices outside the array bounds are valid
            if (model.EntersFromHomeBar(from, isWhite))
            {
                if (to < 0 || to > model.Fields.Length - 1)
                    return false;
            }
            else
            {
                if (from < 0 || from > model.Fields.Length - 1 || to < 0 || to > model.Fields.Length - 1)
                    return false;
            }

            if (!model.EntersFromHomeBar(from, isWhite))
            {
                int fromPoint = model.Fields[from];
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
            }

            // we determine the final block amount
            var blockAmount = model.BlockAmount;
            if (model is IPinModel pinModel)
            {
                // we decrease the block amount if an opponents checker is pinned
                blockAmount -= Math.Abs(pinModel.PinnedFields[to]);
            }

            int toPoint = model.Fields[to];
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

        /// <summary>
        /// Checks if the given player has to play a checker from the homebar.
        /// </summary>
        /// <param name="model">Board to operate on.</param>
        /// <param name="isWhite">Indicates if white or black pieces are relevant.</param>
        /// <returns>Boolean indicating if the next move has to be from the hombebar.</returns>
        public static bool MustEnterFromHomeBar(this IBoardModel model, bool isWhite)
        {
            // if a checker is on the home bar, it has to be moved first
            if (isWhite && model is IHomeBarModel homeBarModelWhite && homeBarModelWhite.HomeBarCountWhite > 0)
            {
                return true;
            }
            else if (!isWhite && model is IHomeBarModel homeBarModelBlack && homeBarModelBlack.HomeBarCountBlack > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given from position is the home bar for the given player.
        /// </summary>
        /// <param name="model">Board to operate on.</param>
        /// <param name="from">The from position of the move.</param>
        /// <param name="isWhite">Indicates if white or black pieces are relevant.</param>
        /// <returns>Boolean indicating if the given from position is the homebar.</returns>
        public static bool EntersFromHomeBar(this IBoardModel model, int from, bool isWhite)
        {
            if (isWhite && model is IHomeBarModel homeBarModel)
            {
                // white pieces enter from the home bar at index -1
                return from == homeBarModel.StartIndexWhite;
            }
            else if (!isWhite && model is IHomeBarModel homeBarModelBlack)
            {
                // black pieces enter from the home bar at index 24
                return from == homeBarModelBlack.StartIndexBlack;
            }
            return false;
        }
    }
}
