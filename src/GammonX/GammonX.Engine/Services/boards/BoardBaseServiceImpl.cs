using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
	/// <summary>
	/// Provides base implementation for board services used in all variants.
	/// </summary>
	internal abstract class BoardBaseServiceImpl : IBoardService
	{
		private readonly MoveSequenceModelComparer _moveSequenceModelComparer = new();

		// <inheritdoc />
		public abstract GameModus Modus { get; }

		// <inheritdoc />
		public abstract IBoardModel CreateBoard();

		// <inheritdoc />
		public ValueTuple<int, int>[] GetLegalMovesAsFlattenedList(IBoardModel model, bool isWhite, params int[] rolls)
		{
			var sequences = GetAllLegalMoveSequences(model, isWhite, rolls);
			var allowed = FilterSequencesByDiceRules(sequences, rolls);
			var moves = ConvertToFlattenedMoves(allowed, model, isWhite);
			return moves.Select(m => new ValueTuple<int, int>(m.From, m.To)).ToArray();
		}

		// <inheritdoc />
		public MoveSequenceModel[] GetLegalMoveSequences(IBoardModel model, bool isWhite, params int[] rolls)
		{
			var sequences = GetAllLegalMoveSequences(model, isWhite, rolls);
			var allowed = FilterSequencesByDiceRules(sequences, rolls);
			var unique = allowed.ToHashSet(_moveSequenceModelComparer);
			return unique.ToArray();
		}

		// <inheritdoc />
		public virtual bool CanMoveChecker(IBoardModel model, int from, int roll, bool isWhite)
		{
			if (CanBearOffChecker(model, from, roll, isWhite))
			{
				return true;
			}

			var newPosition = model.MoveOperator(isWhite, from, roll);
			return model.CanMove(from, newPosition, isWhite);
		}

		// <inheritdoc />
		public virtual bool CanBearOffChecker(IBoardModel model, int from, int roll, bool isWhite)
		{
			return model.CanBearOff(from, roll, isWhite);
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
			if (model is IHomeBarModel)
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
		/// Evaluates all proper <c>from</c> checker fields for the given player and board.
		/// </summary>
		/// <param name="model">Model to operate on.</param>
		/// <param name="isWhite">Indicating if white or black is moving.</param>
		/// <returns>A list of indexes of fields which are moveable.</returns>
		protected virtual IEnumerable<int> GetMoveableCheckerFields(IBoardModel model, bool isWhite)
		{
			var moveableCheckers = new List<int>();
			var homebarCount = GetHomeBarCount(model, isWhite);
			if (homebarCount > 0 && model is IHomeBarModel homeBarModel)
			{
				// if the player has checkers on the home bar, they can only move those checkers
				int startPoint = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
				moveableCheckers.Add(startPoint);

				if (!homeBarModel.MustEnterFromHomebar)
				{
					AddMoveableCheckersFromFields(model, isWhite, moveableCheckers);
				}
			}
			else
			{
				AddMoveableCheckersFromFields(model, isWhite, moveableCheckers);
			}

			return moveableCheckers;
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

		/// <summary>
		/// Applies the dice rules to the given legal moves and returns only those moves.
		/// </summary>
		/// <param name="legalMoves">Legal moves to evaluate.</param>
		/// <param name="rolls">Rolled dices.</param>
		/// <returns>A tuple array containing all legal moves from to.</returns>
		protected virtual ValueTuple<int, int>[] ApplyDiceRules(ValueTuple<int, int>[] legalMoves, int[] rolls)
		{
			// only relevant for two dice rolls
			if (rolls.Length != 2)
				return legalMoves;

			int maxRoll = rolls.Max();
			int combined = rolls.Sum();

			// are there any combined moves (e.g. 0 > 3 for {1,2})
			var combinedMoves = legalMoves
				.Where(m => Math.Abs(m.Item2 - m.Item1) == combined)
				.ToArray();

			if (combinedMoves.Length > 0)
				return combinedMoves;

			// can the rolls be used individually?
			bool canUseRoll1 = legalMoves.Any(m => Math.Abs(m.Item2 - m.Item1) == rolls[0]);
			bool canUseRoll2 = legalMoves.Any(m => Math.Abs(m.Item2 - m.Item1) == rolls[1]);

			// if both rolls can be used, return all legal moves
			if (canUseRoll1 && canUseRoll2)
				return legalMoves;

			// otherwise return only moves with the maximum roll
			return legalMoves
				.Where(m => Math.Abs(m.Item2 - m.Item1) == maxRoll)
				.ToArray();
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

		private static void AddMoveableCheckersFromFields(IBoardModel model, bool isWhite, List<int> moveableCheckers)
		{
			for (int i = 0; i < model.Fields.Length; i++)
			{
				int pointValue = model.Fields[i];
				if ((isWhite && pointValue < 0) || (!isWhite && pointValue > 0))
				{
					moveableCheckers.Add(i);
				}
			}
		}

		private List<MoveSequenceModel> GetAllLegalMoveSequences(IBoardModel model, bool isWhite, int[] rolls)
		{
			var results = new List<MoveSequenceModel>();
			ExploreBoardRecursively(model, isWhite, rolls.ToList(), new List<MoveModel>(), new List<int>(), results);
			return results
				.GroupBy(s => s.SequenceKey())
				.Select(g => g.First())
				.ToList();
		}

		private void ExploreBoardRecursively(
			IBoardModel board,
			bool isWhite,
			List<int> remainingRolls,
			IEnumerable<MoveModel> currentMoves,
			IEnumerable<int> usedDices,
			List<MoveSequenceModel> results)
		{
			bool anyMovePossible = false;
			for (int i = 0; i < remainingRolls.Count; i++)
			{
				int die = remainingRolls[i];
				var possibleMoves = GetMovesForDiceRoll(board, isWhite, die);

				if (possibleMoves.Count == 0)
					continue;

				anyMovePossible = true;

				foreach (var move in possibleMoves)
				{
					var shadowBoard = board.DeepClone();
					MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);

					var newRemaining = new List<int>(remainingRolls);
					newRemaining.RemoveAt(i);

					var newMoves = new List<MoveModel>(currentMoves) { move };
					var newUsedDice = new List<int>(usedDices) { die };

					ExploreBoardRecursively(shadowBoard, isWhite, newRemaining, newMoves, newUsedDice, results);
				}
			}

			if (!anyMovePossible && currentMoves.Any())
			{
				var seq = new MoveSequenceModel();
				currentMoves = ReorderByChains(currentMoves.ToList());
				seq.Moves.AddRange(currentMoves);
				seq.UsedDices.AddRange(usedDices);
				results.Add(seq);
			}
		}

		private List<MoveModel> GetMovesForDiceRoll(IBoardModel board, bool isWhite, int diceRoll)
		{
			var shadowBoard = board.DeepClone();
			var moves = new List<MoveModel>();
			var moveable = GetMoveableCheckerFields(shadowBoard, isWhite);

			foreach (var from in moveable)
			{
				if (CanBearOffChecker(shadowBoard, from, diceRoll, isWhite))
				{
					int to = isWhite ? WellKnownBoardPositions.BearOffWhite : WellKnownBoardPositions.BearOffBlack;
					moves.Add(new MoveModel(from, to));
				}
				else if (CanMoveChecker(shadowBoard, from, diceRoll, isWhite))
				{
					int to = shadowBoard.MoveOperator(isWhite, from, diceRoll);
					moves.Add(new MoveModel(from, to));
				}
			}

			return moves;
		}

		private static List<MoveSequenceModel> FilterSequencesByDiceRules(List<MoveSequenceModel> sequences, int[] rolls)
		{
			// if possible: only sequences that use all dice
			// if not possible: only sequences that use the highest dice
			if (sequences == null || sequences.Count == 0)
				return new List<MoveSequenceModel>();

			int totalDice = rolls.Length;
			int maxRoll = rolls.Max();

			var usingAll = sequences.Where(s => s.UsedDices.Count == totalDice).ToList();
			if (usingAll.Count > 0)
				return usingAll;

			// no sequence has used all the dice -> only sequences are allowed that
			// who have used at least the highest die
			var usingMax = sequences.Where(s => s.UsedDices.Contains(maxRoll)).ToList();
			if (usingMax.Count > 0)
				return usingMax;
			// we return all found moves if the max roll can not be used at all
			return sequences;
		}

		private IEnumerable<MoveModel> ConvertToFlattenedMoves(List<MoveSequenceModel> sequences, IBoardModel model, bool isWhite)
		{
			// flatten sequences into a set of (from,to):
			// add all single moves from allowed sequences
			// additionally add “collapsed” chains (e.g. (0->1,1->3) -> collapsed (0->3)) 
			// only if all moves in the sequence form a continuous chain.
			var flattenedMoves = new HashSet<(int from, int to)>();
			var sequenceCopies = sequences.Select(s => s.DeepClone());
			var initialMovableFields = new HashSet<int>(GetMoveableCheckerFields(model, isWhite));
			foreach (var seq in sequenceCopies)
			{
				var combinedMoves = GetCombinedMoves(seq, initialMovableFields, true);
				foreach (var move in combinedMoves)
				{
					flattenedMoves.Add(move);
				}
			}
			return flattenedMoves.Select(s => new MoveModel(s.from, s.to));
		}

		private static HashSet<(int from, int to)> GetCombinedMoves(
			MoveSequenceModel seq,
			HashSet<int> initialMovableFields,
			bool partialCombinedMoves)
		{
			var set = new HashSet<(int from, int to)>();
			if (seq.Moves.Count >= 2)
			{
				// check whether the moves form a genuine chain of the same checkers.
				bool isContinuousChain = true;
				for (int i = 1; i < seq.Moves.Count; i++)
				{
					if (seq.Moves[i - 1].To != seq.Moves[i].From)
					{
						if (partialCombinedMoves)
						{
							// the sequence may contain a partial continuous chain move
							// substract it from the sequence and handle the remaining moves
							var partialCollapsed = (seq.Moves[0].From, seq.Moves[i - 1].To);
							// only add if it is not a no-op such as (5->5)
							if (partialCollapsed.From != partialCollapsed.To)
							{
								set.Add(partialCollapsed);
								seq.Moves.RemoveRange(0, i);
							}
						}
						isContinuousChain = false;
						break;
					}
				}

				if (isContinuousChain)
				{
					var collapsed = (seq.Moves.First().From, seq.Moves.Last().To);

					// only add if it is not a no-op such as (5->5)
					if (collapsed.From != collapsed.To)
					{
						set.Add(collapsed);
					}

					return set;
				}
			}

			if (partialCombinedMoves)
			{
				// check individual moves and adopt them if necessary
				for (int i = 0; i < seq.Moves.Count; i++)
				{
					if (initialMovableFields.Contains(seq.Moves[i].From))
					{
						var m = seq.Moves[i];
						set.Add((m.From, m.To));
					}
				}
			}

			return set;
		}

		private static List<MoveModel> ReorderByChains(List<MoveModel> moves)
		{
			var result = new List<MoveModel>();
			var remaining = new List<MoveModel>(moves);

			while (remaining.Count > 0)
			{
				// we start with the first move
				var chain = new List<MoveModel> { remaining[0] };
				remaining.RemoveAt(0);

				bool extended;
				do
				{
					extended = false;

					// we search for a move that can be chained at the beginning
					var next = remaining.FirstOrDefault(m => m.From == chain.Last().To);
					if (next != default)
					{
						chain.Add(next);
						remaining.Remove(next);
						extended = true;
					}
				}
				while (extended);

				// Füge die Chain an den Result-Anfang
				result.AddRange(chain);
			}

			return result;
		}
	}
}
