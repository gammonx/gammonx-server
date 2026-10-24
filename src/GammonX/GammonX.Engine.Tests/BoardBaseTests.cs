using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Engine.Tests
{
	public class BoardBaseTests
	{
		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Plakoto)]
		[InlineData(GameModus.Fevga)]
		public void BoardClonePreservesStateAndCopiesHistoryIndependently(GameModus modus)
		{
			var contract = CreateContract();
			contract.Fields[0] = -4;
			contract.Fields[23] = 3;
			contract.PinnedFields![6] = -1;
			contract.BearOffCountWhite = 2;
			contract.BearOffCountBlack = 3;
			contract.HomeBarCountWhite = 4;
			contract.HomeBarCountBlack = 5;
			contract.DoublingCubeValue = 4;
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard(contract);
			service.AddRollEventToHistory(board, true, [2, 3]);
			service.AddRollEventToHistory(board, false, [4, 5]);

			var clone = board.DeepClone();

			Assert.NotSame(board, clone);
			Assert.Equal(board.GetType(), clone.GetType());
			Assert.Equal(board.Modus, clone.Modus);
			Assert.Equal(board.Fields, clone.Fields);
			Assert.NotSame(board.Fields, clone.Fields);
			Assert.Equal(board.BearOffCountWhite, clone.BearOffCountWhite);
			Assert.Equal(board.BearOffCountBlack, clone.BearOffCountBlack);
			Assert.Equal(board.PipCountWhite, clone.PipCountWhite);
			Assert.Equal(board.PipCountBlack, clone.PipCountBlack);
			Assert.NotSame(board.History, clone.History);
			Assert.NotSame(board.History.Events, clone.History.Events);
			var originalEvents = board.History.Events.ToArray();
			var clonedEvents = clone.History.Events.ToArray();
			Assert.Equal(2, clonedEvents.Length);
			for (var eventIndex = 0; eventIndex < originalEvents.Length; eventIndex++)
			{
				Assert.NotSame(originalEvents[eventIndex], clonedEvents[eventIndex]);
				Assert.NotSame(originalEvents[eventIndex].Value, clonedEvents[eventIndex].Value);
				Assert.Equal(originalEvents[eventIndex].ToString(), clonedEvents[eventIndex].ToString());
			}
			var clonedRolls = Assert.IsType<int[]>(clonedEvents[0].Value.GetValue());
			clonedRolls[0] = 99;
			Assert.Equal(new[] { 4, 5 }, Assert.IsType<int[]>(originalEvents[0].Value.GetValue()));
			Assert.IsAssignableFrom<IEditableBoardHistory>(clone.History).TryRemoveLast();
			Assert.Equal(2, board.History.Events.Count);
			Assert.Single(clone.History.Events);

			if (board is IHomeBarModel homeBar)
			{
				var clonedHomeBar = Assert.IsAssignableFrom<IHomeBarModel>(clone);
				Assert.Equal(homeBar.HomeBarCountWhite, clonedHomeBar.HomeBarCountWhite);
				Assert.Equal(homeBar.HomeBarCountBlack, clonedHomeBar.HomeBarCountBlack);
				clonedHomeBar.AddToHomeBar(true, 1);
				Assert.Equal(4, homeBar.HomeBarCountWhite);
			}
			if (board is IPinModel pinModel)
			{
				var clonedPinModel = Assert.IsAssignableFrom<IPinModel>(clone);
				Assert.Equal(pinModel.PinnedFields, clonedPinModel.PinnedFields);
				Assert.NotSame(pinModel.PinnedFields, clonedPinModel.PinnedFields);
				clonedPinModel.PinnedFields[6] = 0;
				Assert.Equal(-1, pinModel.PinnedFields[6]);
			}
			if (board is IDoublingCubeModel cubeModel)
			{
				var clonedCubeModel = Assert.IsAssignableFrom<IDoublingCubeModel>(clone);
				Assert.Equal(cubeModel.DoublingCubeValue, clonedCubeModel.DoublingCubeValue);
				Assert.Equal(cubeModel.DoublingCubeOwner, clonedCubeModel.DoublingCubeOwner);
				clonedCubeModel.DoublingCubeOwner = false;
				Assert.True(cubeModel.DoublingCubeOwner);
			}
			clone.Fields[0] = 99;
			Assert.Equal(-4, board.Fields[0]);
			board.Fields[23] = 98;
			Assert.Equal(3, clone.Fields[23]);
			clone.BearOffChecker(true, 1);
			Assert.Equal(2, board.BearOffCountWhite);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Plakoto)]
		public void RecoverRollBasedOnFromToMove(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			// white checkers
			var roll = board.RecoverRollOperator(true, 0, 5);
			Assert.Equal(5, roll);
			roll = board.RecoverRollOperator(true, 17, 23);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, BoardPositions.HomeBarWhite, 5);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, 18, BoardPositions.BearOffWhite);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, board.StartRangeWhite.Start.Value, BoardPositions.BearOffWhite);
			Assert.Equal(24, roll);
			roll = board.RecoverRollOperator(true, BoardPositions.HomeBarWhite, BoardPositions.BearOffWhite);
			Assert.Equal(25, roll);
			// black checkers
			roll = board.RecoverRollOperator(false, 23, 17);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 6, 0);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, 18);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 5, BoardPositions.BearOffBlack);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, board.StartRangeBlack.Start.Value, BoardPositions.BearOffBlack);
			Assert.Equal(24, roll);
			roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, BoardPositions.BearOffBlack);
			Assert.Equal(25, roll);
		}

		[Theory]
		[InlineData(GameModus.Fevga)]
		public void FevgaRecoverRollBasedOnFromToMove(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			// white checkers
			var roll = board.RecoverRollOperator(true, 0, 5);
			Assert.Equal(5, roll);
			roll = board.RecoverRollOperator(true, 17, 23);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, BoardPositions.HomeBarWhite, 5);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, 18, BoardPositions.BearOffWhite);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(true, board.StartRangeWhite.Start.Value, BoardPositions.BearOffWhite);
			Assert.Equal(24, roll);
			roll = board.RecoverRollOperator(true, BoardPositions.HomeBarWhite, BoardPositions.BearOffWhite);
			Assert.Equal(25, roll);
			// black checkers
			roll = board.RecoverRollOperator(false, 12, 18);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 18, 24);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 18, 2);
			Assert.Equal(8, roll);
			roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, 18);
			Assert.Equal(7, roll);
			roll = board.RecoverRollOperator(false, board.StartRangeBlack.Start.Value, BoardPositions.BearOffBlack);
			Assert.Equal(24, roll);
			roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, BoardPositions.BearOffBlack);
			Assert.Equal(25, roll);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Plakoto)]
		[InlineData(GameModus.Fevga)]
		public void DeserializeBackgammonBoardModel(GameModus modus)
		{
			var contract = CreateContract();

			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard(contract);
			Assert.Equal(contract.BearOffCountBlack, board.BearOffCountBlack);
			Assert.Equal(contract.BearOffCountWhite, board.BearOffCountWhite);
			if (board is IDoublingCubeModel doublingCubeModel)
			{
				Assert.Equal(contract.DoublingCubeOwner, doublingCubeModel.DoublingCubeOwner);
				Assert.Equal(contract.DoublingCubeValue, doublingCubeModel.DoublingCubeValue);
			}
			Assert.Equal(contract.Fields, board.Fields);
			if (board is IHomeBarModel homeBarModel)
			{
				Assert.Equal(contract.HomeBarCountBlack, homeBarModel.HomeBarCountBlack);
				Assert.Equal(contract.HomeBarCountWhite, homeBarModel.HomeBarCountWhite);
			}
			if (board is IPinModel pinModel)
			{
				Assert.Equal(contract.PinnedFields, pinModel.PinnedFields);
			}
			// we see the pip counts as a one way serialization, when deserialized a board
			// always calculates the pip counts based on the fields and homeboard counts
			Assert.NotEqual(contract.PipCountBlack, board.PipCountBlack);
			Assert.NotEqual(contract.PipCountWhite, board.PipCountWhite);
		}

		private static BoardModelContract CreateContract()
		{
			return new BoardModelContract
			{
				BearOffCountBlack = 1,
				BearOffCountWhite = 1,
				DoublingCubeOwner = true,
				DoublingCubeValue = 2,
				Fields = new int[24],
				HomeBarCountBlack = 1,
				HomeBarCountWhite = 1,
				PinnedFields = new int[24],
				PipCountBlack = 365,
				PipCountWhite = 365
			};
		}
	}
}
