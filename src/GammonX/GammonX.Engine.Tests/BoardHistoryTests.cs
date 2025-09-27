using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
	public class BoardHistoryTests
	{
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
			Assert.NotNull(board.History);
			Assert.Empty(board.History.Events);

			var playerStr = isWhite ? "White" : "Black";

			var rollEventValue = new RollEventValueImpl(1, 2, 3, 4);
			Assert.IsType<int[]>(rollEventValue.GetValue());
			Assert.Equal("1 2 3 4", rollEventValue.ToString());
			var rollEvent = new HistoryEventImpl(HistoryEventType.Roll, rollEventValue, isWhite);
			Assert.Equal($"{playerStr} Roll 1 2 3 4", rollEvent.ToString());
			board.History.Add(rollEvent);
			Assert.Single(board.History.Events);

			Assert.Equal(isWhite, rollEvent.IsWhite);
			Assert.Equal(HistoryEventType.Roll, rollEvent.Type);
			Assert.Equal(rollEventValue, rollEvent.Value);

			var moveEventValue = new MoveEventValueImpl(new Tuple<int, int>(5, 2));
			Assert.IsType<Tuple<int, int>[]>(moveEventValue.GetValue());
			Assert.Equal("5/2", moveEventValue.ToString());
			var moveEvent = new HistoryEventImpl(HistoryEventType.Move, moveEventValue, isWhite);
			Assert.Equal($"{playerStr} Move 5/2", moveEvent.ToString());
			board.History.Add(moveEvent);
			Assert.Equal(2, board.History.Events.Count());

			Assert.Equal(isWhite, moveEvent.IsWhite);
			Assert.Equal(HistoryEventType.Move, moveEvent.Type);
			Assert.Equal(moveEventValue, moveEvent.Value);

			var removed = board.History.TryRemoveLast();
			Assert.True(removed);
			Assert.Single(board.History.Events);
			Assert.Contains(rollEvent, board.History.Events);
			Assert.DoesNotContain(moveEvent, board.History.Events);

			board.History.Remove(rollEvent);
			Assert.Empty(board.History.Events);
			Assert.DoesNotContain(rollEvent, board.History.Events);

			removed = board.History.TryRemoveLast();
			Assert.False(removed);
		}
	}
}
