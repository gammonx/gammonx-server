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
