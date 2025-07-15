using GammonX.Engine.Models;
using GammonX.Engine.Services;
using Moq;

namespace GammonX.Engine.Tests
{
    public class HomeBarServiceTests
    {
        #region Simple Interface Tests

        [Theory]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void HasNoHomeBarAndCanNotHit(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var homeBarModel = boardModel as IHomeBarModel;
            Assert.Null(homeBarModel);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void HasHomeBarAndCanHit(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var homeBarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homeBarModel);
            Assert.Equal(0, homeBarModel.StartIndexWhite);
            Assert.Equal(23, homeBarModel.StartIndexBlack);
        }

        #endregion Simple Interface Tests

        #region Simple Hit Testts

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void WhiteCheckerHitsBlack(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var board = service.CreateBoard();
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            var mock = new Mock<IBoardModel>();

            mock.SetupGet(b => b.Fields).Returns(BackgammonHitBoard);
            mock.SetupGet(b => b.MoveOperator).Returns(board.MoveOperator);
            mock.SetupGet(b => b.BlockAmount).Returns(board.BlockAmount);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexWhite).Returns(homeBarModel.StartIndexWhite);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexBlack).Returns(homeBarModel.StartIndexBlack);
            mock.As<IHomeBarModel>().SetupGet(b => b.HomeBarCountWhite).Returns(() => homeBarModel.HomeBarCountWhite);
            mock.As<IHomeBarModel>().SetupGet(b => b.HomeBarCountBlack).Returns(() => homeBarModel.HomeBarCountBlack);
            mock.As<IHomeBarModel>()
                .Setup(x => x.AddToHomeBar(It.IsAny<bool>(), It.IsAny<int>()))
                .Callback<bool, int>((isWhite, amount) => homeBarModel.AddToHomeBar(isWhite, amount));

            var mockBoard = mock.Object;

            Assert.True(service.CanMoveChecker(mockBoard, 8, 7, true));
            Assert.True(service.CanMoveChecker(mockBoard, 9, 5, true));
            Assert.False(service.CanMoveChecker(mockBoard, 10, 3, true));
            Assert.True(service.CanMoveChecker(mockBoard, 11, 1, true));

            // hit the black checkers and sent them to the home bar
            service.MoveRoll(mockBoard, 8, 7, true);
            service.MoveRoll(mockBoard, 9, 5, true);
            Assert.Throws<InvalidOperationException>(() => service.MoveRoll(mockBoard, 10, 3, true));
            service.MoveRoll(mockBoard, 11, 1, true);

            Assert.Equal(3, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(-1, mockBoard.Fields[15]);
            Assert.Equal(-1, mockBoard.Fields[14]);
            Assert.Equal(-1, mockBoard.Fields[12]);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void BlackCheckerHitsWhite(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var board = service.CreateBoard();
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            var mock = new Mock<IBoardModel>();

            mock.SetupGet(b => b.Fields).Returns(BackgammonHitBoard);
            mock.SetupGet(b => b.MoveOperator).Returns(board.MoveOperator);
            mock.SetupGet(b => b.BlockAmount).Returns(board.BlockAmount);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexWhite).Returns(homeBarModel.StartIndexWhite);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexBlack).Returns(homeBarModel.StartIndexBlack);
            mock.As<IHomeBarModel>().SetupGet(b => b.HomeBarCountWhite).Returns(() => homeBarModel.HomeBarCountWhite);
            mock.As<IHomeBarModel>().SetupGet(b => b.HomeBarCountBlack).Returns(() => homeBarModel.HomeBarCountBlack);
            mock.As<IHomeBarModel>()
                .Setup(x => x.AddToHomeBar(It.IsAny<bool>(), It.IsAny<int>()))
                .Callback<bool, int>((isWhite, amount) => homeBarModel.AddToHomeBar(isWhite, amount));

            var mockBoard = mock.Object;

            Assert.True(service.CanMoveChecker(mockBoard, 15, 7, false));
            Assert.True(service.CanMoveChecker(mockBoard, 14, 5, false));
            Assert.False(service.CanMoveChecker(mockBoard, 13, 3, false));
            Assert.True(service.CanMoveChecker(mockBoard, 12, 1, false));

            // hit the black checkers and sent them to the home bar
            service.MoveRoll(mockBoard, 15, 7, false);
            service.MoveRoll(mockBoard, 14, 5, false);
            Assert.Throws<InvalidOperationException>(() => service.MoveRoll(mockBoard, 13, 3, false));
            service.MoveRoll(mockBoard, 12, 1, false);

            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(3, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, mockBoard.Fields[8]);
            Assert.Equal(1, mockBoard.Fields[9]);
            Assert.Equal(1, mockBoard.Fields[11]);
        }

        private readonly int[] BackgammonHitBoard = new int[24]
        {
            -5, // 0 – Black Home
            -5, // 1 – Black Home
             0, // 2 – Black Home
             0, // 3 - Black Home
             0, // 4 - Black Home
             0, // 5 - Black Home
             0, // 6
             0, // 7
            -1, // 8
            -1, // 9
            -2, // 10
            -1, // 11
             1, // 12
             2, // 13
             1, // 14
             1, // 15
             0, // 16
             0, // 17
             0, // 18 – White Home
             0, // 19 – White Home
             0, // 20 – White Home
             0, // 21 – White Home
             5, // 22 – White Home
             5  // 23 - White Home
        };

        #endregion
    }
}
