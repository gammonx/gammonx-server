
namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class BoardHistoryImpl : IBoardHistory
	{
		private readonly List<IHistoryEvent> _history;

		// <inheritdoc />
		public IEnumerable<IHistoryEvent> Events => _history;

		public BoardHistoryImpl()
		{
			_history = new List<IHistoryEvent>();
		}

		// <inheritdoc />
		public void Add(IHistoryEvent historyEvent)
		{
			_history.Add(historyEvent);
		}

		// <inheritdoc />
		public void Remove(IHistoryEvent historyEvent)
		{
			_history.Remove(historyEvent);
		}

		// <inheritdoc />
		public bool TryRemoveLast()
		{
			var lastEvent = _history.LastOrDefault();
			if (lastEvent != null)
			{
				var index = _history.IndexOf(lastEvent);
				_history.RemoveAt(index);
				return true;
			}
			return false;
		}
	}
}
