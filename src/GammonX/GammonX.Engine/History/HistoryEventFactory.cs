using GammonX.Engine.Models;

using GammonX.Models.Enums;

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

		public static IHistoryEvent CreateHitEvent(bool isWhite, int from)
		{
			var hitEventValue = new HitEventValueImpl(from, isWhite);
			return new HistoryEventImpl(HistoryEventType.Hit, hitEventValue, isWhite);
		}

		public static IHistoryEvent CreateCubeEvent(bool isWhite, CubeAction cubeAction)
		{
			var cubeEventValue = new CubeEventValueImpl(cubeAction);
            return new HistoryEventImpl(HistoryEventType.Cube, cubeEventValue, isWhite);
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
