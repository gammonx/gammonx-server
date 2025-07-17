using GammonX.Engine.Models;
namespace GammonX.Engine.Services
{
	/// <summary>
	/// Provides base implementation for board services used in all variants.
	/// </summary>
	internal abstract class BoardBaseServiceImpl : IBoardService
	{
		// <inheritdoc />
		public abstract GameModus Modus { get; }

		// <inheritdoc />
		public abstract IBoardModel CreateBoard();

		// <inheritdoc />
		public ValueTuple<int, int>[] GetLegalMoves(IBoardModel model, bool isWhite, params int[] rolls)
		{
			// TODO: UNIT TESTS for home bar (entering)
			// TODO: UNIT TESTS for blocked (already tested within CanMoveChecker)
			// TODO: UNIT TESTS for pinned (already tested within CanMoveChecker)
			// TODO: UNIT TESTS for hitting (already tested within CanMoveChecker)
			// TODO: UNIT TESTS for bearing off (already tested within CanBearOff)

			var legalMoves = new List<(int from, int to)>();
			// get only points with relevance for the current player
			var playerPoints = new List<int>();
			var homebarCount = GetHomeBarCount(model, isWhite);
			if (homebarCount > 0 && model is IHomeBarModel homeBarModel)
			{
				// if the player has checkers on the home bar, they can only move those checkers
				int startPoint = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
				playerPoints.Add(startPoint);
			}
			else
			{
				for (int i = 0; i < model.Fields.Length; i++)
				{
					int pointValue = model.Fields[i];
					if ((isWhite && pointValue < 0) || (!isWhite && pointValue > 0))
					{
						playerPoints.Add(i);
					}
				}
			}

			// we currently return moves based on a double distinctly
			// after every move, we remove the used roll and recalculate the legal moves
			// based on the current board state
			var distinctRolls = rolls.Distinct().ToArray();

			foreach (var roll in distinctRolls)
			{
				foreach (var from in playerPoints)
				{
					if (model.CanBearOff(from, roll, isWhite))
					{
						int to = isWhite ? WellKnownBoardPositions.BearOffWhite : WellKnownBoardPositions.BearOffBlack;
						legalMoves.Add((from, to));
					}
					else if (CanMoveChecker(model, from, roll, isWhite))
					{
						int to = model.MoveOperator(isWhite, from, roll);
						legalMoves.Add((from, to));
					}
				}
			}

			return legalMoves.ToArray();
		}

		// <inheritdoc />
		public virtual bool CanMoveChecker(IBoardModel model, int from, int roll, bool isWhite)
		{
			if (model.CanBearOff(from, roll, isWhite))
			{
				return true;
			}

			var newPosition = model.MoveOperator(isWhite, from, roll);
			return model.CanMove(from, newPosition, isWhite);
		}

		// <inheritdoc />
		public virtual void MoveCheckerTo(IBoardModel model, int from, int to, bool isWhite)
		{
			// we check first if the given from to move bears the checker off
			if (CheckerBearedOff(model, from, to, isWhite))
			{
				return;
			}

			// we check it here because most of the variants actually use hitting
			var homeBarModel = model as IHomeBarModel;
			if (homeBarModel != null)
			{
				// we check if we would hit an opponents checker with this move
				EvaluateHittedCheckers(model, from, to, isWhite);
			}

			if (isWhite)
			{
				if (model.EntersFromHomeBar(from, isWhite))
				{
					((IHomeBarModel)model).RemoveFromHomeBar(isWhite, 1);
					// add a negative checker to the new position
					model.Fields.SetValue(model.Fields[to] -= 1, to);
				}
				else
				{
					// remove a negative checker from the old position
					model.Fields.SetValue(model.Fields[from] += 1, from);
					// add a negative checker to the new position
					model.Fields.SetValue(model.Fields[to] -= 1, to);
				}
			}
			else
			{
				if (model.EntersFromHomeBar(from, isWhite))
				{
					((IHomeBarModel)model).RemoveFromHomeBar(isWhite, 1);
					// add a positive checker to the new position
					model.Fields.SetValue(model.Fields[to] += 1, to);
				}
				else
				{
					// remove a positive checker from the old position
					model.Fields.SetValue(model.Fields[from] -= 1, from);
					// add a positive checker to the new position
					model.Fields.SetValue(model.Fields[to] += 1, to);
				}
			}
		}

		// <inheritdoc />
		bool IBoardService.MoveChecker(IBoardModel model, int from, int roll, bool isWhite)
		{
			// we probably can remove this later on
			if (!CanMoveChecker(model, from, roll, isWhite))
				throw new InvalidOperationException("Cannot move checker to the specified position.");

			try
			{
				var newPosition = model.MoveOperator(isWhite, from, roll);
				MoveCheckerTo(model, from, newPosition, isWhite);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error moving checker: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Evaluates if a checker on the target field is hit by the current move.
		/// </summary>
		/// <remarks>
		/// Can be overridden in derived classes to implement specific logic for determining a hit.
		/// </remarks>
		/// <param name="model">Model to operate on.</param>
		/// <param name="from">From move position.</param>
		/// <param name="to">To move position. Can contain an opponents checker.</param>
		/// <param name="isWhite">Indicates if white or black checker.</param>
		protected virtual void EvaluateHittedCheckers(IBoardModel model, int from, int to, bool isWhite)
		{
			// we know that the opponents checker can be hit, otherwise the move could not have been made
			if (isWhite)
			{
				// we detect a black checker on the target field
				if (model.Fields[to] > 0)
				{
					HitChecker(model, to, isWhite);
				}
			}
			else
			{
				// we detect a white checker on the target field
				if (model.Fields[to] < 0)
				{
					HitChecker(model, to, isWhite);
				}
			}
		}

		/// <summary>
		/// Is executed when a checker is hit by the current move.
		/// </summary>
		/// <remarks>
		/// Can be overridden in derived classes to implement specific logic for hitting a checker.
		/// </remarks>
		/// <param name="model">Model to operate on.</param>
		/// <param name="fieldIndex">Field index where the hit occurs.</param>
		/// <param name="isWhite">Indicates if white or black checker.</param>
		/// <exception cref="InvalidOperationException">Throws if given model does not support hitting.</exception>
		protected virtual void HitChecker(IBoardModel model, int fieldIndex, bool isWhite)
		{
			if (model is IHomeBarModel homeBarModel)
			{
				if (isWhite)
				{
					// we remove the black checker from the moveable fields array
					model.Fields.SetValue(model.Fields[fieldIndex] -= 1, fieldIndex);
					// and move it to the black home bar
					homeBarModel.AddToHomeBar(!isWhite, 1);
				}
				else
				{
					// we remove the white checker from the moveable fields array
					model.Fields.SetValue(model.Fields[fieldIndex] += 1, fieldIndex);
					// and move it to the white home bar
					homeBarModel.AddToHomeBar(!isWhite, 1);
				}
			}
			else
			{
				throw new InvalidOperationException("Model does not support hitting checkers.");
			}
		}

		/// <summary>
		/// Checks if the given move to <c>to</c> would bear it off the board.
		/// </summary>
		/// <param name="model">Model to operate on.</param>
		/// <param name="to">To position value.</param>
		/// <param name="isWhite">Indicates if white or black checker.</param>
		/// <returns>Boolean indicating if the to position bears off.</returns>
		protected virtual bool IsBearOffMove(IBoardModel model, int to, bool isWhite)
		{
			if (isWhite && to == WellKnownBoardPositions.BearOffWhite)
				return true;
			if (!isWhite && to == WellKnownBoardPositions.BearOffBlack)
				return true;
			return false;
		}

		private static bool CheckerBearedOff(IBoardModel model, int from, int to, bool isWhite)
		{
			if (isWhite)
			{
				if (to == WellKnownBoardPositions.BearOffWhite)
				{
					// we remove a negative checker from the old position
					model.Fields.SetValue(model.Fields[from] += 1, from);
					// and bear off the checker
					model.BearOffChecker(isWhite, 1);
					return true;
				}
			}
			else
			{
				if (to == WellKnownBoardPositions.BearOffBlack)
				{
					// we remove a positive checker from the old position
					model.Fields.SetValue(model.Fields[from] -= 1, from);
					// and bear off the checker
					model.BearOffChecker(isWhite, 1);
					return true;
				}
			}
			return false;
		}

		private static int GetHomeBarCount(IBoardModel model, bool isWhite)
		{
			if (model is IHomeBarModel homebarBoard)
			{
				return isWhite ? homebarBoard.HomeBarCountWhite : homebarBoard.HomeBarCountBlack;
			}
			return 0;
		}

	}
}
