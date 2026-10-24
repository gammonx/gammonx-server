using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Engine.Tests
{
	public class BoardHistoryTests
	{
		[Theory]
		[InlineData(1, 2)]
		[InlineData(6, 6)]
		public void RollEventValueCloneCopiesRollArray(int firstRoll, int secondRoll)
		{
			var value = new RollEventValueImpl(firstRoll, secondRoll);

			var clone = Assert.IsType<RollEventValueImpl>(value.Clone());

			Assert.NotSame(value, clone);
			var originalRolls = Assert.IsType<int[]>(value.GetValue());
			var clonedRolls = Assert.IsType<int[]>(clone.GetValue());
			Assert.Equal(originalRolls, clonedRolls);
			Assert.NotSame(originalRolls, clonedRolls);
			clonedRolls[0] = 99;
			Assert.Equal(firstRoll, originalRolls[0]);
			originalRolls[1] = 98;
			Assert.Equal(secondRoll, clonedRolls[1]);
		}

		[Fact]
		public void MoveEventValueCloneCopiesMoveArray()
		{
			var value = new MoveEventValueImpl(Tuple.Create(0, 2), Tuple.Create(2, 5));

			var clone = Assert.IsType<MoveEventValueImpl>(value.Clone());

			Assert.NotSame(value, clone);
			var originalMoves = Assert.IsType<Tuple<int, int>[]>(value.GetValue());
			var clonedMoves = Assert.IsType<Tuple<int, int>[]>(clone.GetValue());
			Assert.Equal(originalMoves, clonedMoves);
			Assert.NotSame(originalMoves, clonedMoves);
			clonedMoves[0] = Tuple.Create(10, 12);
			Assert.Equal(Tuple.Create(0, 2), originalMoves[0]);
			originalMoves[1] = Tuple.Create(12, 15);
			Assert.Equal(Tuple.Create(2, 5), clonedMoves[1]);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void HitEventValueClonePreservesBar(bool isWhite)
		{
			var value = new HitEventValueImpl(5, isWhite);

			var clone = Assert.IsType<HitEventValueImpl>(value.Clone());

			Assert.NotSame(value, clone);
			Assert.Equal(value.GetValue(), clone.GetValue());
			var hit = Assert.IsType<Tuple<int, int>>(clone.GetValue());
			Assert.Equal(5, hit.Item1);
			Assert.Equal(isWhite ? BoardPositions.HomeBarWhite : BoardPositions.HomeBarBlack, hit.Item2);
			Assert.Equal(value.ToString(), clone.ToString());
		}

		[Theory]
		[InlineData(CubeAction.Offer)]
		[InlineData(CubeAction.Take)]
		[InlineData(CubeAction.Pass)]
		public void CubeEventValueClonePreservesAction(CubeAction action)
		{
			var value = new CubeEventValueImpl(action);

			var clone = Assert.IsType<CubeEventValueImpl>(value.Clone());

			Assert.NotSame(value, clone);
			Assert.Equal(action, Assert.IsType<CubeAction>(clone.GetValue()));
			Assert.Equal(value.ToString(), clone.ToString());
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void BoardHistoryClonePreservesOrderAndCopiesEvents(bool isWhite)
		{
			var history = new BoardHistoryImpl();
			history.Add(HistoryEventFactory.CreateRollEvent(isWhite, 2, 3));
			history.Add(HistoryEventFactory.CreateMoveEvent(isWhite, [new MoveModel(0, 2)]));
			history.Add(HistoryEventFactory.CreateHitEvent(isWhite, 2));
			history.Add(HistoryEventFactory.CreateCubeEvent(isWhite, CubeAction.Offer));

			var clone = Assert.IsType<BoardHistoryImpl>(history.Clone());

			Assert.NotSame(history, clone);
			Assert.NotSame(history.Events, clone.Events);
			var originalEvents = history.Events.ToArray();
			var clonedEvents = clone.Events.ToArray();
			Assert.Equal(4, clonedEvents.Length);
			Assert.Equal(
				new[] { HistoryEventType.Cube, HistoryEventType.Hit, HistoryEventType.Move, HistoryEventType.Roll },
				clonedEvents.Select(historyEvent => historyEvent.Type));
			for (var eventIndex = 0; eventIndex < originalEvents.Length; eventIndex++)
			{
				var originalEvent = originalEvents[eventIndex];
				var clonedEvent = clonedEvents[eventIndex];
				Assert.NotSame(originalEvent, clonedEvent);
				Assert.Equal(originalEvent.GetType(), clonedEvent.GetType());
				Assert.Equal(originalEvent.Type, clonedEvent.Type);
				Assert.Equal(originalEvent.IsWhite, clonedEvent.IsWhite);
				Assert.NotSame(originalEvent.Value, clonedEvent.Value);
				Assert.Equal(originalEvent.Value.GetType(), clonedEvent.Value.GetType());
				Assert.Equal(originalEvent.ToString(), clonedEvent.ToString());
			}
			var clonedRolls = Assert.IsType<int[]>(clonedEvents[^1].Value.GetValue());
			clonedRolls[0] = 99;
			Assert.Equal(new[] { 2, 3 }, Assert.IsType<int[]>(originalEvents[^1].Value.GetValue()));
			Assert.True(clone.TryRemoveLast());
			Assert.True(clone.TryPeekLast(out var lastEvent));
			Assert.NotNull(lastEvent);
			Assert.Equal(HistoryEventType.Hit, lastEvent.Type);
			Assert.Equal(4, history.Events.Count);
			history.Add(HistoryEventFactory.CreateRollEvent(!isWhite, 4, 5));
			Assert.Equal(3, clone.Events.Count);
		}

		[Fact]
		public void BoardHistoryCloneOfEmptyHistoryIsIndependent()
		{
			var history = new BoardHistoryImpl();

			var clone = Assert.IsType<BoardHistoryImpl>(history.Clone());

			Assert.Empty(clone.Events);
			Assert.NotSame(history.Events, clone.Events);
			Assert.False(clone.TryRemoveLast());
			clone.Add(HistoryEventFactory.CreateRollEvent(true, 1, 2));
			Assert.Single(clone.Events);
			Assert.Empty(history.Events);
		}

		[Theory]
		[InlineData(GameModus.Backgammon, true)]
		[InlineData(GameModus.Tavla, true)]
		[InlineData(GameModus.Portes, true)]
		[InlineData(GameModus.Plakoto, true)]
		[InlineData(GameModus.Fevga, true)]
		[InlineData(GameModus.Backgammon, false)]
		[InlineData(GameModus.Tavla, false)]
		[InlineData(GameModus.Portes, false)]
		[InlineData(GameModus.Plakoto, false)]
		[InlineData(GameModus.Fevga, false)]
		public void CanAddAndRemoveBoardHistoryEvents(GameModus modus, bool isWhite)
		{
			var boardService = BoardServiceFactory.Create(modus);
			var board = boardService.CreateBoard();
			var edtiableHistory = board.History as IEditableBoardHistory;
			Assert.NotNull(edtiableHistory);
			Assert.NotNull(board.History);
			Assert.Empty(board.History.Events);

			var playerStr = isWhite ? "White" : "Black";

			var rollEventValue = new RollEventValueImpl(1, 2, 3, 4);
			Assert.IsType<int[]>(rollEventValue.GetValue());
			Assert.Equal("1 2 3 4", rollEventValue.ToString());
			var rollEvent = new HistoryEventImpl(HistoryEventType.Roll, rollEventValue, isWhite);
			Assert.Equal($"{playerStr} Roll 1 2 3 4", rollEvent.ToString());
			edtiableHistory.Add(rollEvent);
			Assert.Single(board.History.Events);

			Assert.Equal(isWhite, rollEvent.IsWhite);
			Assert.Equal(HistoryEventType.Roll, rollEvent.Type);
			Assert.Equal(rollEventValue, rollEvent.Value);

			var moveEventValue = new MoveEventValueImpl(new Tuple<int, int>(5, 2));
			Assert.IsType<Tuple<int, int>[]>(moveEventValue.GetValue());
			Assert.Equal("5/2", moveEventValue.ToString());
			var moveEvent = new HistoryEventImpl(HistoryEventType.Move, moveEventValue, isWhite);
			Assert.Equal($"{playerStr} Move 5/2", moveEvent.ToString());
			edtiableHistory.Add(moveEvent);
			Assert.Equal(2, board.History.Events.Count);

			Assert.Equal(isWhite, moveEvent.IsWhite);
			Assert.Equal(HistoryEventType.Move, moveEvent.Type);
			Assert.Equal(moveEventValue, moveEvent.Value);

			var removed = edtiableHistory.TryRemoveLast();
			Assert.True(removed);
			Assert.Single(board.History.Events);
			Assert.Contains(rollEvent, board.History.Events);
			Assert.DoesNotContain(moveEvent, board.History.Events);

			edtiableHistory.Remove(rollEvent);
			Assert.Empty(board.History.Events);
			Assert.DoesNotContain(rollEvent, board.History.Events);

			removed = edtiableHistory.TryRemoveLast();
			Assert.False(removed);
		}

		[Fact]
		public void CanCreateMoveEvent()
		{
			var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = boardService.CreateBoard();
			Assert.NotNull(board.History);
			Assert.Empty(board.History.Events);
			var moveModel1 = new MoveModel(0, 5);
			var moveModel2 = new MoveModel(5, 10);
			var moveEvent = HistoryEventFactory.CreateMoveEvent(true, new[] { moveModel1, moveModel2 });
			Assert.True(moveEvent.IsWhite);
			Assert.Equal(HistoryEventType.Move, moveEvent.Type);
			Assert.IsType<Tuple<int, int>[]>(moveEvent.Value.GetValue());
			Assert.Equal("0/5 5/10", moveEvent.Value.ToString());
		}

		[Fact]
		public void CanCreateRollEvent()
		{
			var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = boardService.CreateBoard();
			Assert.NotNull(board.History);
			Assert.Empty(board.History.Events);
			var rollEvent = HistoryEventFactory.CreateRollEvent(true, 1, 1, 1, 1);
			Assert.True(rollEvent.IsWhite);
			Assert.Equal(HistoryEventType.Roll, rollEvent.Type);
			Assert.IsType<int[]>(rollEvent.Value.GetValue());
			Assert.Equal($"1 1 1 1", rollEvent.Value.ToString());
		}

		[Fact]
		public void CanCreateHitEvent()
		{
			var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = boardService.CreateBoard();
			Assert.NotNull(board.History);
			Assert.Empty(board.History.Events);
			var hitEvent = HistoryEventFactory.CreateHitEvent(true, 5);
			Assert.True(hitEvent.IsWhite);
			Assert.Equal(HistoryEventType.Hit, hitEvent.Type);
			Assert.IsType<Tuple<int, int>>(hitEvent.Value.GetValue());
			Assert.Equal($"5/bar", hitEvent.Value.ToString());
		}

		[Fact]
		public void CanCreateCubeEvents()
		{
			var offerCubeEvent = HistoryEventFactory.CreateCubeEvent(true, CubeAction.Offer);
			Assert.True(offerCubeEvent.IsWhite);
			Assert.Equal(HistoryEventType.Cube, offerCubeEvent.Type);
			Assert.IsType<CubeAction>(offerCubeEvent.Value.GetValue());
			Assert.Equal($"White Cube Offer", offerCubeEvent.ToString());

			var takeCubeEvent = HistoryEventFactory.CreateCubeEvent(true, CubeAction.Take);
			Assert.True(takeCubeEvent.IsWhite);
			Assert.Equal(HistoryEventType.Cube, takeCubeEvent.Type);
			Assert.IsType<CubeAction>(takeCubeEvent.Value.GetValue());
			Assert.Equal($"White Cube Take", takeCubeEvent.ToString());

			var passCubeEvent = HistoryEventFactory.CreateCubeEvent(true, CubeAction.Pass);
			Assert.True(passCubeEvent.IsWhite);
			Assert.Equal(HistoryEventType.Cube, passCubeEvent.Type);
			Assert.IsType<CubeAction>(passCubeEvent.Value.GetValue());
			Assert.Equal($"White Cube Pass", passCubeEvent.ToString());
		}

		[Theory]
		[InlineData(CubeAction.Double)]
		[InlineData(CubeAction.NoDouble)]
		[InlineData(CubeAction.TooGood)]
		[InlineData(CubeAction.Unknown)]
		public void CannotCreateCubeEventWithInvalidCubeAction(CubeAction invalidAction)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => HistoryEventFactory.CreateCubeEvent(true, invalidAction));
		}
	}
}
