
namespace GammonX.Engine.Models
{
	/// <summary>
	/// Provides some standard functionality for the most common board models.
	/// </summary>
	internal abstract class BoardBaseImpl : IBoardModel
	{
		// <inheritdoc />
		public abstract GameModus Modus { get; }

		// <inheritdoc />
		public abstract int[] Fields { get; protected set; }

		// <inheritdoc />
		public abstract Range HomeRangeWhite { get; }

		// <inheritdoc />
		public abstract Range HomeRangeBlack { get; }

		// <inheritdoc />
		public virtual int BearOffCountWhite { get; protected set; } = 0;

		// <inheritdoc />
		public virtual int BearOffCountBlack { get; protected set; } = 0;

		// <inheritdoc />
		public abstract int BlockAmount { get; }

		// <inheritdoc />
		public virtual Func<bool, int, int, int> MoveOperator => new Func<bool, int, int, int>((isWhite, currentPosition, moveDistance) =>
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
		public virtual Func<bool, int, int, bool> CanBearOffOperator => new Func<bool, int, int, bool>((isWhite, currentPosition, moveDistance) =>
		{
			if (isWhite)
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				return to > HomeRangeWhite.End.Value;
			}
			else
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				return to < HomeRangeBlack.End.Value;
			}
		});

		// <inheritdoc />
		public virtual Func<bool, int, bool> IsInHomeOperator => new Func<bool, int, bool>((isWhite, position) =>
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
	}
}
