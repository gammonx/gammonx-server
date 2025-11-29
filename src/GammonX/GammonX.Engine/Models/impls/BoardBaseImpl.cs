
using GammonX.Engine.History;

using GammonX.Models.Enums;

namespace GammonX.Engine.Models
{
	/// <summary>
	/// Provides some standard functionality for the most common board models.
	/// </summary>
	internal abstract class BoardBaseImpl : IBoardModel, ICloneable
	{
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
		public virtual Range StartRangeWhite => new(0, 5);

		// <inheritdoc />
		public virtual Range StartRangeBlack => new(23, 18);

		// <inheritdoc />
		public virtual int BearOffCountWhite { get; protected set; } = 0;

		// <inheritdoc />
		public virtual int BearOffCountBlack { get; protected set; } = 0;

		// <inheritdoc />
		public virtual int WinConditionCount => 15;

		// <inheritdoc />
		public abstract int BlockAmount { get; }

		// <inheritdoc />
		public int PipCountWhite => GetPipCount(true);

		// <inheritdoc />
		public int PipCountBlack => GetPipCount(true);

		// <inheritdoc />
		public virtual Func<bool, int, int, int> MoveOperator => new((isWhite, currentPosition, moveDistance) =>
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
		});

		// <inheritdoc />
		public virtual Func<bool, int, int, int> RecoverRollOperator => new((isWhite, from, to) =>
		{
			if (isWhite)
			{
				// white moves from 0 to 23
				if (to == WellKnownBoardPositions.BearOffWhite)
				{
					to = HomeRangeWhite.End.Value + 1;
				}
				int roll = to - from;
				return roll;
			}
			else
			{
				// black moves forward (wraps from 23 -> 0)
				if (to == WellKnownBoardPositions.BearOffBlack)
				{
					to = HomeRangeBlack.End.Value;
					int bearOffRoll = from - to + 1;
					return bearOffRoll;
				}
				int roll = from - to;
				return roll;
			}
		});

		// <inheritdoc />
		public virtual Func<bool, int, int, bool> CanBearOffOperator => new((isWhite, currentPosition, moveDistance) =>
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
		});

		// <inheritdoc />
		public virtual Func<bool, int, bool> IsInHomeOperator => new((isWhite, position) =>
		{
			if (isWhite && (position < HomeRangeWhite.Start.Value || position > HomeRangeWhite.End.Value)) return false;
			if (!isWhite && (position > HomeRangeBlack.Start.Value || position < HomeRangeBlack.End.Value)) return false;
			return true;
		});

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

		protected virtual int GetPipCount(bool isWhite)
		{
			var fieldsCopy = Fields.ToList();
			var pipCount = 0;
			if (isWhite)
			{
				pipCount += GetPipeCountForBoard(fieldsCopy.ToArray(), HomeRangeWhite.End.Value, (i) => i < 0);
				if (this is IHomeBarModel homeBar)
				{
					pipCount += homeBar.HomeBarCountWhite * 24;
				}
			}
			else
			{
				pipCount += GetPipeCountForBoard(fieldsCopy.ToArray(), HomeRangeBlack.End.Value, (i) => i > 0);
				if (this is IHomeBarModel homeBar)
				{
					pipCount += homeBar.HomeBarCountBlack * 24;
				}
			}
			return pipCount;
		}

		protected static int GetPipeCountForBoard(int[] fields, int homeRangeEndIndex, Func<int, bool> valueChecker)
		{
			var pipCount = 0;
			for (int i = 0; i < fields.Length; i++)
			{
				var checkers = fields[i];
				if (valueChecker.Invoke(checkers))
				{
					int distance = homeRangeEndIndex + 1 - i;
					pipCount += Math.Abs(checkers * distance);
				}
			}
			return pipCount;
		}
	}
}
