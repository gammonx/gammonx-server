using GammonX.Engine.Models;
using GammonX.Engine.Services;
using Moq;

namespace GammonX.Server.Tests.Utils
{
	internal static class BoardUtils
	{
		public static void SetFields(this IBoardModel board, int[] fields)
		{
			if (board is BoardBaseImpl boardBase)
			{
				boardBase.SetFields(fields);
			}
			else
			{
				throw new InvalidOperationException("SetFields can only be used on BoardBaseImpl instances.");
			}
		}

		public static Mock<IBoardService> CreateBoardServiceMock(GameModus modus)
		{
			var boardService = BoardServiceFactory.Create(modus);
			var boardServiceMock = new Mock<IBoardService>();
			boardServiceMock.Setup(x => x.Modus).Returns(modus);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.MoveCheckerTo(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Callback<IBoardModel, int, int, bool>((model, from, roll, isWhite) => boardService.MoveCheckerTo(model, from, roll, isWhite));
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.MoveChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>((model, from, roll, isWhite) => boardService.MoveChecker(model, from, roll, isWhite));
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.GetLegalMovesAsFlattenedList(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int[]>((model, isWhite, rolls) => boardService.GetLegalMovesAsFlattenedList(model, isWhite, rolls));
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.GetLegalMoveSequences(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int[]>((model, isWhite, rolls) => boardService.GetLegalMoveSequences(model, isWhite, rolls));
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.CanBearOffChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>((model, from, roll, isWhite) => boardService.CanBearOffChecker(model, from, roll, isWhite));
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.CanMoveChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>((model, from, roll, isWhite) => boardService.CanMoveChecker(model, from, roll, isWhite));
			return boardServiceMock;
		}
	}
}
