namespace GammonX.Engine.History
{
	/// <summary>
	/// Represents an event that can happen during a board game.
	/// </summary>
	public interface IHistoryEvent
	{
		/// <summary>
		/// Gets a boolean indicating if the white or black checkers was involved in the event
		/// </summary>
		bool IsWhite { get; }

		/// <summary>
		/// Gets the type of this history event.
		/// </summary>
		HistoryEventType Type { get; }

		/// <summary>
		/// Gets the value of type <see cref="IHistoryEventValue"/> for this history event.
		/// </summary>
		/// <remarks>
		/// Can be e.g. be two rolls <c>2</c>/<c>6</c> or multiple moves <c>8/10</c>/<c>8/14</c>.
		/// </remarks>
		IHistoryEventValue Value { get; }
	}

	public enum HistoryEventType
	{
		Roll = 0,
		Move = 1,
	}
}
