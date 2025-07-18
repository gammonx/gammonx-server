using GammonX.Engine.Models;
using GammonX.Engine.Services;

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

			if (boardModel is IHomeBarModel homeBarModel)
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

			if (inverted is IHomeBarModel invertedHomeBarModel)
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
	}
}
