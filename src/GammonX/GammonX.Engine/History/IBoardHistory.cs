namespace GammonX.Engine.History
{
	/// <summary>
	/// Provides the capabilities to log all dice rolls and board movements.
	/// </summary>
	public interface IBoardHistory
	{
		/// <summary>
		/// Gets the list of events of this board history.
		/// </summary>
		IEnumerable<IHistoryEvent> Events { get; }

		/// <summary>
		/// Adds the given <paramref name="historyEvent"/> to <see cref="Events"/>.
		/// </summary>
		/// <param name="historyEvent">Event to add.</param>
		void Add(IHistoryEvent historyEvent);

		/// <summary>
		/// Removes the given <paramref name="historyEvent"/> from <see cref="Events"/>.
		/// </summary>
		/// <param name="historyEvent">Event to remove.</param>
		void Remove(IHistoryEvent historyEvent);

		/// <summary>
		/// Tries to removes the last entry from <see cref="Events"/>
		/// </summary>
		/// <returns>True if remove. False if not.</returns>
		bool TryRemoveLast();
	}
}
