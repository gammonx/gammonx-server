using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests.Stubs
{
	internal class TavliStartGameSessionFactoryStub : IGameSessionFactory
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();

		public IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			// dice service only returns a roll of 2 and 3
			var diceService = _diceServiceFactory.Create();
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			var boardService = BoardServiceFactory.Create(modus);

			return new GameSessionImpl(
				matchId,
				modus,
				boardService,
				mock.Object
			);
		}
	}

	internal class TavliEndGameSessionFactoryStub : IGameSessionFactory
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();

		public IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			// dice service only returns a roll of 2 and 3
			var diceService = _diceServiceFactory.Create();
			var diceServiceMock = new Mock<IDiceService>();
			diceServiceMock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);

			// create boards which only needs one move to finish the game
			Mock<IBoardService> boardServiceMock = BoardUtils.CreateBoardServiceMock(modus);

			if (modus == GameModus.Portes)
			{
				var portesBoard = new PortesBoardModelImpl();
				// leave one white checker remaining on the board
				portesBoard.BearOffChecker(true, 14);
				portesBoard.SetFields(new int[24]);
				portesBoard.Fields[23] = -1;
				boardServiceMock.Setup(x => x.CreateBoard()).Returns(portesBoard);

				return new GameSessionImpl(
					matchId,
					modus,
					boardServiceMock.Object,
					diceServiceMock.Object
				);
			}
			else if (modus == GameModus.Plakoto)
			{
				var plakotoBoard = new PlakotoBoardModelImpl();
				// leave one white checker remaining on the board
				plakotoBoard.BearOffChecker(true, 14);
				plakotoBoard.SetFields(new int[24]);
				plakotoBoard.Fields[23] = -1;
				boardServiceMock.Setup(x => x.CreateBoard()).Returns(plakotoBoard);

				return new GameSessionImpl(
					matchId,
					modus,
					boardServiceMock.Object,
					diceServiceMock.Object
				);
			}
			else if (modus == GameModus.Fevga)
			{
				var fevgaBoard = new FevgaBoardModelImpl();
				// leave one white checker remaining on the board
				fevgaBoard.BearOffChecker(true, 14);
				fevgaBoard.SetFields(new int[24]);
				fevgaBoard.Fields[23] = -1;
				boardServiceMock.Setup(x => x.CreateBoard()).Returns(fevgaBoard);

				return new GameSessionImpl(
					matchId,
					modus,
					boardServiceMock.Object,
					diceServiceMock.Object
				);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
	}
}
