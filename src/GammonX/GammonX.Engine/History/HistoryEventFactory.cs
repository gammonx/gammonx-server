using GammonX.Engine.Models;

namespace GammonX.Engine.History
{
	public static class HistoryEventFactory
	{
		public static IHistoryEvent CreateRollEvent(bool isWhite, params int[] rolls)
		{
			var rollEventValue = new RollEventValueImpl(rolls);
			return new HistoryEventImpl(HistoryEventType.Roll, rollEventValue, isWhite);
		}

		public static IHistoryEvent CreateMoveEvent(bool isWhite, params MoveModel[] model)
		{
			var tuples = model.Select(m => new Tuple<int, int>(m.From, m.To)).ToArray();
			var moveEventValue = new MoveEventValueImpl(tuples);
			return new HistoryEventImpl(HistoryEventType.Move, moveEventValue, isWhite);
		}
	}

	public static class BoardHistoryFactory
	{
		public static IBoardHistory CreateEmpty()
		{
			return new BoardHistoryImpl();
		}
	}
}
