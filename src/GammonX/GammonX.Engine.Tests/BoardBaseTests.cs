using GammonX.Engine.Models;
using GammonX.Engine.Services;

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
			// black checkers
			roll = board.RecoverRollOperator(false, 23, 17);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 6, 0);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, 18);
			Assert.Equal(6, roll);
			roll = board.RecoverRollOperator(false, 5, BoardPositions.BearOffBlack);
			Assert.Equal(6, roll);
		}
	}
}
