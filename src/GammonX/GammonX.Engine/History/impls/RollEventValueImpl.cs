namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class RollEventValueImpl : IHistoryEventValue
	{
		private readonly int[] _rolls;

		public RollEventValueImpl(params int[] rolls)
		{
			_rolls = rolls;
		}

		// <inheritdoc />
		public object GetValue()
		{
			return _rolls;
		}

		/// <summary>
		/// Converts the <see cref="GetValue"/> into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
		{
			return string.Join(' ', _rolls);
		}
	}
}
