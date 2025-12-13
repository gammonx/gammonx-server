using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Models.gameSession;
using GammonX.Server.Services;

using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests.Stubs
{
	public class RandomGameSessionBoardFactory : IGameSessionFactory
	{
		private readonly Mock<IBoardService> _boardServiceMock;
		private readonly IDiceServiceFactory _diceFactory;

		public RandomGameSessionBoardFactory(
			IDiceServiceFactory diceServiceFactory,
			GameModus modus,
			int[] fields,
			int bearOffCountBlack,
			int bearOffCountWhite,
			int homebarCountBlack,
			int homebarCountWhite
			)
		{
			_diceFactory = diceServiceFactory;

			IBoardModel boardModel;
			if (modus == GameModus.Backgammon)
			{
				boardModel = new BackgammonBoardModelImpl();
			}
			else if (modus == GameModus.Portes)
			{
				boardModel = new PortesBoardModelImpl();
			}
			else if (modus == GameModus.Tavla)
			{
				boardModel = new TavlaBoardModelImpl();
			}
			else
			{
				throw new InvalidOperationException("game modus not supported yet");
			}

			_boardServiceMock = BoardUtils.CreateBoardServiceMock(modus);
			_boardServiceMock.Setup(x => x.CreateBoard()).Returns(boardModel);
			
			boardModel.SetFields(fields);
			boardModel.BearOffChecker(true, bearOffCountWhite);
			boardModel.BearOffChecker(false, bearOffCountBlack);
			((IHomeBarModel)boardModel).AddToHomeBar(true, homebarCountWhite);
			((IHomeBarModel)boardModel).AddToHomeBar(false, homebarCountBlack);
		}

		public IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			if (modus == GameModus.Plakoto)
			{
				return new PlakotoGameSession(
					matchId,
					modus,
					_boardServiceMock.Object,
					_diceFactory.Create(DiceServiceType.Crypto)
				);
			}
			else if (modus == GameModus.Backgammon)
			{
				return new BackgammonGameSession(
					matchId,
					modus,
					_boardServiceMock.Object,
					_diceFactory.Create(DiceServiceType.Crypto)
				);
			}
			else
			{
				return new GameSessionImpl(
					matchId,
					modus,
					_boardServiceMock.Object,
					_diceFactory.Create(DiceServiceType.Crypto)
				);
			}
		}
	}
}
