
namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class BoardHistoryImpl : IBoardHistory
	{
		private Stack<IHistoryEvent> _history;

		// <inheritdoc />
		public Stack<IHistoryEvent> Events => _history;

		public BoardHistoryImpl()
		{
			_history = new Stack<IHistoryEvent>();
		}

		// <inheritdoc />
		public void Add(IHistoryEvent historyEvent)
		{
			_history.Push(historyEvent);
		}

		// <inheritdoc />
		public void Remove(IHistoryEvent historyEvent)
		{
			var history = _history.ToList();
			history.Remove(historyEvent);
			_history = new Stack<IHistoryEvent>(history);
		}

		// <inheritdoc />
		public bool TryRemoveLast()
		{
			return _history.TryPop(out var _);
		}

		// <inheritdoc />
		public bool TryPeekLast(out IHistoryEvent? lastEvent)
		{
			return _history.TryPeek(out lastEvent);
		}
	}
}
