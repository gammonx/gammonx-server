using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

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
				.Setup(x => x.CanUseAnyRoll(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int[]>(boardService.CanUseAnyRoll);
			boardServiceMock
				.Setup(x => x.ResolveMoveSequence(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int, int, int[]>(boardService.ResolveMoveSequence);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.MoveCheckerTo(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Callback<IBoardModel, int, int, bool>(boardService.MoveCheckerTo);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.MoveChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>(boardService.MoveChecker);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.GetLegalMovesAsFlattenedList(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int[]>(boardService.GetLegalMovesAsFlattenedList);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.GetLegalMoveSequences(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
				.Returns<IBoardModel, bool, int[]>(boardService.GetLegalMoveSequences);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.CanBearOffChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>(boardService.CanBearOffChecker);
			boardServiceMock
				.As<IBoardService>()
				.Setup(x => x.CanMoveChecker(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
				.Returns<IBoardModel, int, int, bool>(boardService.CanMoveChecker);
			return boardServiceMock;
		}
	}
}
