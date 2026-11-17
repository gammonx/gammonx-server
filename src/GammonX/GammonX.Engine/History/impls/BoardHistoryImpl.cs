
namespace GammonX.Engine.History
{
	// <inheritdoc />
	internal sealed class BoardHistoryImpl : IBoardHistory, IEditableBoardHistory, ICloneable
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

		// <inheritdoc />
		public object Clone()
		{
			// Built-in event values are immutable; only the stack needs its own storage.
			// Preserve defensive cloning for custom event implementations.
			var historyClone = _history.Select(e => e is HistoryEventImpl
                {
                    Value: RollEventValueImpl or MoveEventValueImpl or HitEventValueImpl or CubeEventValueImpl
                }
                ? e
                : (IHistoryEvent)((ICloneable)e).Clone()).Reverse();

			return new BoardHistoryImpl()
			{
				_history = new Stack<IHistoryEvent>(historyClone)
			};
		}
	}
}
