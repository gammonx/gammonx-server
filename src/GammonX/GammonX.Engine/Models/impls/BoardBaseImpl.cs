using GammonX.Engine.History;
using GammonX.Models.Enums;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace GammonX.Engine.Models
{
	/// <summary>
	/// Provides some standard functionality for the most common board models.
	/// </summary>
	internal abstract class BoardBaseImpl : IBoardModel, ICloneable, IEquatable<IBoardModel>
	{
		private readonly Func<bool, int, int, int> _recoverRollOperator;
		private readonly Func<bool, int, bool> _isInHomeOperator;

		protected BoardBaseImpl()
		{
			_recoverRollOperator = RecoverRoll;
			_isInHomeOperator = IsInHomeRange;
		}

		// <inheritdoc />
		public abstract GameModus Modus { get; }

		// <inheritdoc />
		public abstract int[] Fields { get; protected set; }

		// <inheritdoc />
		public IBoardHistory History { get; } = new BoardHistoryImpl();

		// <inheritdoc />
		public abstract Range HomeRangeWhite { get; }

		// <inheritdoc />
		public abstract Range HomeRangeBlack { get; }

		// <inheritdoc />
		public virtual Range StartRangeWhite { get; } = new(0, 5);

		// <inheritdoc />
		public virtual Range StartRangeBlack { get; } = new(23, 18);

		// <inheritdoc />
		public virtual int BearOffCountWhite { get; protected set; }

		// <inheritdoc />
		public virtual int BearOffCountBlack { get; protected set; }

		// <inheritdoc />
		public virtual int WinConditionCount => 15;

		// <inheritdoc />
		public abstract int BlockAmount { get; }

		// <inheritdoc />
		public int PipCountWhite => GetPipCount(true);

		// <inheritdoc />
		public int PipCountBlack => GetPipCount(false);

		// <inheritdoc />
		public virtual Func<bool, int, int, int> MoveOperator => Move;

		private static int Move(bool isWhite, int currentPosition, int moveDistance)
		{
			if (isWhite)
			{
				// White moves from 0 to 23
				int newPosition = currentPosition + moveDistance;
				return newPosition;
			}
			else
			{
				// Black moves from 23 to 0
				int newPosition = currentPosition - moveDistance;
				return newPosition;
			}
		}

		// <inheritdoc />
		public virtual Func<bool, int, int, int> RecoverRollOperator => _recoverRollOperator;

		private int RecoverRoll(bool isWhite, int from, int to)
		{
			if (isWhite)
			{
				// white moves from 0 to 23
				if (to == BoardPositions.BearOffWhite)
				{
					return HomeRangeWhite.End.Value + 1 - from;
				}

				return to - from;
			}
			else
			{
				// black moves forward (wraps from 23 -> 0)
				if (to == BoardPositions.BearOffBlack)
				{
					return from - HomeRangeBlack.End.Value + 1;
				}

				return from - to;
			}
		}

		// <inheritdoc />
		public virtual Func<bool, int, int, bool> CanBearOffOperator => CanBearOff;

		private bool CanBearOff(bool isWhite, int currentPosition, int moveDistance)
		{
			if (isWhite)
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				// checkers with the perfect bear off roll can always be taken out
				if (to == HomeRangeWhite.End.Value + 1)
				{
					return true;
				}
				// checkers with a higher roll than their bear off value can only be taken off
				// if there does not exist a checker with a higher index/distance.
				else if (to > HomeRangeWhite.End.Value)
				{
					// check if there are any checkers in the home range with above the current position
					bool highestCheckerIndex = !Fields
						.Skip(HomeRangeWhite.Start.Value)
						.Take(currentPosition - HomeRangeWhite.Start.Value)
						.Any(v => v < 0);
					return highestCheckerIndex;
				}

				return false;
			}
			else
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				// checkers with the perfect bear off roll can always be taken out
				if (to == HomeRangeBlack.End.Value - 1)
				{
					return true;
				}
				// checkers with a higher roll than their bear off value can only be taken off
				// if there does not exist a checker with a lower index/distance.
				else if (to < HomeRangeBlack.End.Value)
				{
					// check if there are any checkers in the home range with above the current position
					bool highestCheckerIndex = !Fields
						.Skip(currentPosition + 1)
						.Any(v => v > 0);
					return highestCheckerIndex;
				}

				return false;
			}
		}

		// <inheritdoc />
		public virtual Func<bool, int, bool> IsInHomeOperator => _isInHomeOperator;

		private bool IsInHomeRange(bool isWhite, int position)
		{
			var homeRange = isWhite ? HomeRangeWhite : HomeRangeBlack;
			var start = homeRange.Start.Value;
			var end = homeRange.End.Value;
			return isWhite
				? position >= start && position <= end
				: position <= start && position >= end;
		}

		// <inheritdoc />
		public virtual Func<bool, int, bool> IsInStartOperator => IsInStartRange;

		private bool IsInStartRange(bool isWhite, int position)
		{
			if (isWhite && (position < StartRangeWhite.Start.Value || position > StartRangeWhite.End.Value))
			{
				return false;
			}

			if (!isWhite && (position > StartRangeBlack.Start.Value || position < StartRangeBlack.End.Value))
			{
				return false;
			}

			return true;
		}

		// <inheritdoc />
		public virtual void BearOffChecker(bool isWhite, int amount)
		{
			if (isWhite)
			{
				BearOffCountWhite += amount;
			}
			else
			{
				BearOffCountBlack += amount;
			}
		}

		// <inheritdoc />
		public abstract IBoardModel InvertBoard();

		/// <summary>
		/// Overwrites the <see cref="Fields"/> property.
		/// </summary>
		/// <remarks>
		/// Marked as internal and should only be used for unit test purposes.
		/// </remarks>
		/// <param name="fields">Fields array to set.</param>
		public void SetFields(int[] fields)
		{
			fields.CopyTo(Fields, 0);
		}

		// <inheritdoc />
		public abstract object Clone();

		// <inheritdoc />
		public bool Equals(IBoardModel? other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other is not BoardBaseImpl otherBoard || Modus != otherBoard.Modus)
			{
				return false;
			}

			return Fields.SequenceEqual(otherBoard.Fields)
			       && BearOffCountWhite == otherBoard.BearOffCountWhite
			       && BearOffCountBlack == otherBoard.BearOffCountBlack
			       && EqualsVariantState(otherBoard);
		}

		// <inheritdoc />
		public override bool Equals(object? obj)
		{
			return obj is IBoardModel other && Equals(other);
		}

		// <inheritdoc />
		public override int GetHashCode()
		{
			var bearOffCountWhite = BearOffCountWhite;
			var bearOffCountBlack = BearOffCountBlack;
			var hash = new HashCode();
			hash.Add(Modus);
			AddArrayHash(ref hash, Fields);
			hash.Add(bearOffCountWhite);
			hash.Add(bearOffCountBlack);
			AddVariantStateHash(ref hash);
			return hash.ToHashCode();
		}

        /// <summary>
        /// Determines whether the variant-specific state of this board is equal to that of another board.
        /// </summary>
        /// <param name="other">The other board to compare against.</param>
        /// <returns>True if the variant-specific state is equal; otherwise, false.</returns>
		protected abstract bool EqualsVariantState(BoardBaseImpl other);

        /// <summary>
        /// Adds the variant-specific state of this board to the provided hash code.
        /// </summary>
        /// <param name="hash">The hash code to which the variant-specific state will be added.</param>
		protected abstract void AddVariantStateHash(ref HashCode hash);
        
        /// <summary>
        /// Adds the hash of an array of integers to the provided hash code.
        /// </summary>
        /// <param name="hash">The hash code to which the array's hash will be added.</param>
        /// <param name="values">The array of integers whose hash will be added.</param>
		protected static void AddArrayHash(ref HashCode hash, int[] values)
		{
			hash.Add(values.Length);
			foreach (var value in values)
			{
				hash.Add(value);
			}
		}

        /// <summary>
        /// Calculates the pip count for the specified player (white or black) on the board, including checkers on the home bar.
        /// </summary>
        /// <param name="isWhite">Indicates whether to calculate the pip count for the white player (true) or the black player (false).</param>
        /// <returns>The total pip count for the specified player.</returns>
		protected virtual int GetPipCount(bool isWhite)
		{
			var pipCount = 0;
			if (isWhite)
			{
				pipCount += GetPipeCountForBoard(isWhite, Fields, HomeRangeWhite.End.Value, (i) => i < 0);
				if (this is IHomeBarModel homeBar)
				{
					pipCount += homeBar.HomeBarCountWhite * 24;
				}
			}
			else
			{
				pipCount += GetPipeCountForBoard(isWhite, Fields, HomeRangeBlack.End.Value, (i) => i > 0);
				if (this is IHomeBarModel homeBar)
				{
					pipCount += homeBar.HomeBarCountBlack * 24;
				}
			}

			return pipCount;
		}

        /// <summary>
        /// Calculates the pip count for the specified player on the given board fields, considering only the checkers that satisfy the value checker.
        /// </summary>
        /// <param name="isWhite">Indicates whether to calculate the pip count for the white player (true) or the black player (false).</param>
        /// <param name="fields">The array of board fields representing the checkers' positions.</param>
        /// <param name="homeRangeEndIndex">The index of the end of the home range for the player.</param>
        /// <param name="valueChecker">A function that determines which checkers should be included in the pip count.</param>
        /// <returns>The total pip count for the specified player on the given board fields.</returns>
		protected int GetPipeCountForBoard(bool isWhite, int[] fields, int homeRangeEndIndex, Func<int, bool> valueChecker)
		{
			var pipCount = 0;
			for (int i = 0; i < fields.Length; i++)
			{
				var checkers = fields[i];
				if (valueChecker.Invoke(checkers))
				{
					int distance = RecoverRollOperator.Invoke(isWhite, i, homeRangeEndIndex);
					pipCount += Math.Abs(checkers) * (1 + Math.Abs(distance));
				}
			}

			return pipCount;
		}
	}
}
