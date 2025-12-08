using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Engine.Tests
{
	public class BoardInversionTests
	{
		[Theory]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Fevga)]
		[InlineData(GameModus.Plakoto)]
		[InlineData(GameModus.Backgammon)]
		public void StartBoardIsInverted(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var boardModel = service.CreateBoard();
			boardModel.BearOffChecker(true, 2);
			boardModel.BearOffChecker(false, 3);

			if (boardModel is IDoublingCubeModel doublingCubeModel)
			{
				Assert.False(doublingCubeModel.DoublingCubeOwner);
			}

			if (boardModel is IHomeBarModel homeBarModel && homeBarModel.CanSendToHomeBar)
			{
				homeBarModel.AddToHomeBar(true, 2);
				homeBarModel.AddToHomeBar(false, 3);
			}

			var expected = new int[24];
			boardModel.Fields.CopyTo(expected, 0);

			var inverted = boardModel.InvertBoard();
			Assert.Equal(expected, inverted.Fields);
			Assert.Equal(3, inverted.BearOffCountWhite);
			Assert.Equal(2, inverted.BearOffCountBlack);

			if (inverted is IDoublingCubeModel invertedDoublingCubeModel)
			{
				Assert.True(invertedDoublingCubeModel.DoublingCubeOwner);
			}

			if (inverted is IHomeBarModel invertedHomeBarModel && invertedHomeBarModel.CanSendToHomeBar)
			{
				Assert.Equal(3, invertedHomeBarModel.HomeBarCountWhite);
				Assert.Equal(2, invertedHomeBarModel.HomeBarCountBlack);
			}

			if (boardModel is IPinModel pinModel)
			{
				var expectedPinned = new int[24];
				pinModel.PinnedFields.CopyTo(expected, 0);
				var invertedPinModel = boardModel.InvertBoard() as IPinModel;
				Assert.NotNull(invertedPinModel);
				Assert.Equal(expectedPinned, invertedPinModel.PinnedFields);
			}
		}

		[Theory]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Plakoto)]
		[InlineData(GameModus.Backgammon)]
		public void InvertFields(GameModus modus)
		{
			// NOTE :: fevga board has its own inversion mechanism
			var service = BoardServiceFactory.Create(modus);
			var boardModel = service.CreateBoard();
			var expected = new int[24];
			boardModel.Fields.CopyTo(expected, 0);
			var invertedFields = BoardBroker.InvertBoardFields(boardModel.Fields);
			Assert.Equal(expected, invertedFields);
		}

		[Theory]
		[InlineData(0, 5, 23, 18)]
		[InlineData(5, 10, 18, 13)]
		[InlineData(23, 21, 0, 2)]
		[InlineData(10, 0, 13, 23)]
		[InlineData(BoardPositions.HomeBarWhite, 5, BoardPositions.HomeBarBlack, 18)]
		[InlineData(5, BoardPositions.BearOffWhite, 18, BoardPositions.BearOffBlack)]
		[InlineData(BoardPositions.HomeBarBlack, 18, BoardPositions.HomeBarWhite, 5)]
		[InlineData(18, BoardPositions.BearOffBlack, 5, BoardPositions.BearOffWhite)]
		public void InvertBoardMoveHorizontallyShouldMapCorrectly(int from, int to, int expectedFrom, int expectedTo)
		{
			var (actualFrom, actualTo) = BoardBroker.InvertBoardMoveHorizontally(from, to);
			Assert.Equal(expectedFrom, actualFrom);
			Assert.Equal(expectedTo, actualTo);
		}

		[Theory]
		[InlineData(0, 5)]
		[InlineData(5, 10)]
		[InlineData(23, 21)]
		[InlineData(BoardPositions.HomeBarWhite, 5)]
		[InlineData(5, BoardPositions.BearOffWhite)]
		[InlineData(BoardPositions.HomeBarBlack, 18)]
		[InlineData(18, BoardPositions.BearOffBlack)]
		public void InvertBoardMoveHorizontallyShouldBeSymmetric(int from, int to)
		{
			// invert twice should yield the original move
			var invertedOnce = BoardBroker.InvertBoardMoveHorizontally(from, to);
			var invertedTwice = BoardBroker.InvertBoardMoveHorizontally(invertedOnce.Item1, invertedOnce.Item2);
			Assert.Equal(from, invertedTwice.Item1);
			Assert.Equal(to, invertedTwice.Item2);
		}

		[Theory]
		[InlineData(13, 15, 1, 3)]
		[InlineData(22, 2, 10, 14)]
		[InlineData(0, 2, 12, 14)]
		[InlineData(11, 15, 23, 3)]
		[InlineData(BoardPositions.HomeBarWhite, 5, BoardPositions.HomeBarBlack, 17)]
		[InlineData(BoardPositions.BearOffWhite, BoardPositions.BearOffBlack, BoardPositions.BearOffBlack, BoardPositions.BearOffWhite)]
		public void InvertHorizontalDiagonalBoardMoveShouldMapCorrectly(int from, int to, int expectedFrom, int expectedTo)
		{
			var (actualFrom, actualTo) = BoardBroker.InvertBoardMoveDiagonalHorizontally(from, to);
			Assert.Equal(expectedFrom, actualFrom);
			Assert.Equal(expectedTo, actualTo);
		}

		[Theory]
		[InlineData(13, 15)]
		[InlineData(22, 2)]
		[InlineData(0, 2)]
		[InlineData(11, 15)]
		[InlineData(BoardPositions.HomeBarWhite, 5)]
		public void InvertHorizontalDiagonalBoardMoveShouldBeSymmetric(int from, int to)
		{
			var invertedOnce = BoardBroker.InvertBoardMoveDiagonalHorizontally(from, to);
			var invertedTwice = BoardBroker.InvertBoardMoveDiagonalHorizontally(invertedOnce.Item1, invertedOnce.Item2);
			Assert.Equal(from, invertedTwice.Item1);
			Assert.Equal(to, invertedTwice.Item2);
		}
	}
}
