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
			return string.Join(' ', _moves.Select(m => $"{m.Item1}/{m.Item2}"));
		}
	}
}
