using GammonX.Engine.Models;

using GammonX.Models.Enums;

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

		/// <summary>
		/// Inverts the given fields array
		/// </summary>
		/// <remarks>
		/// There are 24 fields, numbered from 0 to 23.
		/// Field i is mirrored to 23 - i.
		/// A positive value(e.g. +3, i.e. 3 black checkers) becomes -3 (3 white checkers) and vice versa.
		/// </remarks>
		/// <param name="originalFields"></param>
		/// <returns>Inverted board fields array</returns>
		public static int[] InvertBoardFields(int[] originalFields)
		{
			int[] invertedFields = new int[originalFields.Length];
			for (int i = 0; i < originalFields.Length; i++)
			{
				int sourceIndex = (originalFields.Length - 1) - i;
				invertedFields[i] = -originalFields[sourceIndex];
			}
			return invertedFields;
		}

		public static (int, int) InvertFromToMove(GameModus modus, int from, int to)
		{
			var invertDiagonalHorinzotally = modus == GameModus.Fevga;
			if (invertDiagonalHorinzotally)
			{
				return InvertBoardMoveDiagonalHorizontally(from, to);
			}
			else
			{
				return InvertBoardMoveHorizontally(from, to);
			}
		}

		/// <summary>
		/// Inverts the given from/to move horizontally (left > right/right > left).
		/// </summary>
		/// <remarks>
		/// E.g. black move from 23 to 21 becomes white move from 0 to 2.
		/// </remarks>
		/// <param name="from">From field index.</param>
		/// <param name="to">To field index.</param>
		/// <returns>Converted value tuple with from/to as values.</returns>
		public static (int, int) InvertBoardMoveHorizontally(int from, int to)
		{
			static int InvertIndexHorizontal(int index)
			{
				return index switch
				{
					BoardPositions.HomeBarWhite => BoardPositions.HomeBarBlack,
					BoardPositions.HomeBarBlack => BoardPositions.HomeBarWhite,
					BoardPositions.BearOffWhite => BoardPositions.BearOffBlack,
					BoardPositions.BearOffBlack => BoardPositions.BearOffWhite,
					_ => (23 - index)
				};
			}

			return (InvertIndexHorizontal(from), InvertIndexHorizontal(to));
		}

		/// <summary>
		/// Inverts the given from/to move horizontally (left > right/right > left).
		/// </summary>
		/// <remarks>
		/// black move 13 > 15 inverts to white move 0 > 2 
        /// black move 22 > 2 inverts to white move 11 > 15
		/// </remarks>
		/// <param name="from">From field index.</param>
		/// <param name="to">To field index.</param>
		/// <returns>Converted value tuple with from/to as values.</returns>
		public static (int, int) InvertBoardMoveDiagonalHorizontally(int from, int to)
		{
			int InvertHorinzotalDiagonalIndex(int index)
			{
				return index switch
				{
					BoardPositions.HomeBarWhite => BoardPositions.HomeBarBlack,
					BoardPositions.HomeBarBlack => BoardPositions.HomeBarWhite,
					BoardPositions.BearOffWhite => BoardPositions.BearOffBlack,
					BoardPositions.BearOffBlack => BoardPositions.BearOffWhite,
					< 0 => index,       // in case of unexpected negatives
					< 12 => index + 12, // top half > bottom half
					_ => index - 12     // bottom half > top half
				};
			}

			return (InvertHorinzotalDiagonalIndex(from), InvertHorinzotalDiagonalIndex(to));
		}

		/// <summary>
		/// Creates a deep clone of the given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">Board model to clone.</param>
		/// <returns>A deep cloned model.</returns>
		/// <exception cref="InvalidOperationException">If <paramref name="model"/> does not implement <see cref="ICloneable"/>.</exception>
		public static IBoardModel DeepClone(this IBoardModel model)
        {
            if (model is ICloneable cloneable)
            {
                return (IBoardModel)cloneable.Clone();
            }
            else
            {
                throw new InvalidOperationException("The board model does not support cloning.");
			}
		}

        /// <summary>
        /// Searches all legal <c>to</c> positions based on the given <paramref name="moveSequences"/> and <paramref name="from"/>.
        /// </summary>
        /// <param name="from">From position to move from.</param>
        /// <param name="moveSequences">Legal move sequences.</param>
        /// <returns>A list of valid <c>to</c> positions.</returns>
		public static HashSet<int> GetLegalToPositions(int from, IEnumerable<MoveSequenceModel> moveSequences)
		{
			var results = new HashSet<int>();
			foreach (var seq in moveSequences)
			{
				if (seq.Moves.Count == 0)
					continue;
				// search for all moves in sequence which starts from
				for (int i = 0; i < seq.Moves.Count; i++)
				{
					var move = seq.Moves[i];
					if (move.From != from)
						continue;

					results.Add(move.To); // first move is valid by definition
					int currentTo = move.To; // check for chained/combined moves
					for (int j = i + 1; j < seq.Moves.Count; j++)
					{
						var nextMove = seq.Moves[j];
						if (nextMove.From == currentTo)
						{
							results.Add(nextMove.To);
							currentTo = nextMove.To;
						}
						else // no chained there or broke up
							break;
					}
				}
			}
			return results;
		}
	}
}
