using GammonX.Engine.Models;

namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class MoveEventValueImpl : IHistoryEventValue
	{
		private readonly IEnumerable<Tuple<int, int>> _moves;

		public MoveEventValueImpl(params Tuple<int, int>[] moves)
		{
			_moves = moves;
		}

		// <inheritdoc />
		public object GetValue()
		{
			return _moves;
		}

		/// <summary>
		/// Converts the <see cref="GetValue"/> into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
		{
			return string.Join(' ', _moves.Select(m => $"{Convert(m.Item1)}/{Convert(m.Item2)}"));
		}

		private static string Convert(int position)
		{
			if (position == WellKnownBoardPositions.BearOffWhite || position == WellKnownBoardPositions.BearOffBlack)
			{
				return "off";
			}
			if (position == WellKnownBoardPositions.HomeBarWhite || position == WellKnownBoardPositions.HomeBarBlack)
			{
				return "bar";
			}
			return $"{position}";
		}
	}
}
