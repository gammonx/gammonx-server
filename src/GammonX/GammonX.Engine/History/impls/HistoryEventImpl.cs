namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class HistoryEventImpl : IHistoryEvent
	{
		// <inheritdoc />
		public bool IsWhite { get; private set; }

		// <inheritdoc />
		public HistoryEventType Type { get; private set; }

		// <inheritdoc />
		public IHistoryEventValue Value { get; private set; }

		public HistoryEventImpl(HistoryEventType type, IHistoryEventValue value, bool isWhite)
		{
			IsWhite = isWhite;
			Type = type;
			Value = value;
		}

		/// <summary>
		/// Converts this history event into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
		{
			var player = IsWhite ? "White" : "Black";
			return $"{player} {Type} {Value}";
		}
	}
}
