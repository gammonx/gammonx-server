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
            Assert.Equal(-1, homeBarModel.StartIndexWhite);
            Assert.Equal(24, homeBarModel.StartIndexBlack);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void CanAddToAndRemoveFromHomeBar(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var homeBarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homeBarModel);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            homeBarModel.AddToHomeBar(true, 1);
            homeBarModel.AddToHomeBar(false, 1);
            Assert.Equal(1, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, homeBarModel.HomeBarCountWhite);
            homeBarModel.AddToHomeBar(true, 10);
            homeBarModel.AddToHomeBar(false, 10);
            Assert.Equal(11, homeBarModel.HomeBarCountWhite);
            Assert.Equal(11, homeBarModel.HomeBarCountWhite);
            homeBarModel.RemoveFromHomeBar(true, 1);
            homeBarModel.RemoveFromHomeBar(false, 1);
            Assert.Equal(10, homeBarModel.HomeBarCountWhite);
            Assert.Equal(10, homeBarModel.HomeBarCountWhite);
            homeBarModel.RemoveFromHomeBar(true, 10);
            homeBarModel.RemoveFromHomeBar(false, 10);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
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
            service.MoveChecker(mockBoard, 8, 7, true);
            service.MoveChecker(mockBoard, 9, 5, true);
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(mockBoard, 10, 3, true));
            service.MoveChecker(mockBoard, 11, 1, true);

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

            // hit the white checkers and sent them to the home bar
            service.MoveChecker(mockBoard, 15, 7, false);
            service.MoveChecker(mockBoard, 14, 5, false);
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(mockBoard, 13, 3, false));
            service.MoveChecker(mockBoard, 12, 1, false);

            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(3, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, mockBoard.Fields[8]);
            Assert.Equal(1, mockBoard.Fields[9]);
            Assert.Equal(1, mockBoard.Fields[11]);
        }

        #endregion Simple Hit Testts

        #region CanMove OnHomeBarCount Tests

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void MustMoveBlackCheckerFromHomeIfHit(GameModus gameModus)
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
            mock.As<IHomeBarModel>()
                .Setup(x => x.RemoveFromHomeBar(It.IsAny<bool>(), It.IsAny<int>()))
                .Callback<bool, int>((isWhite, amount) => homeBarModel.RemoveFromHomeBar(isWhite, amount));

            var mockBoard = mock.Object;

            // hit the black checkers and sent them to the home bar
            service.MoveChecker(mockBoard, 8, 7, true);
            Assert.Equal(1, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(-1, mockBoard.Fields[15]);
            // the hitted black checker should be removed from the field
            Assert.Equal(14, mockBoard.Fields.Where(f => f > 0).Sum());
            // cannot move all other checkers to empty field 7
            Assert.False(service.CanMoveChecker(mockBoard, 14, 7, false));
            Assert.False(service.CanMoveChecker(mockBoard, 13, 8, false));
            Assert.False(service.CanMoveChecker(mockBoard, 12, 9, false));
            Assert.False(service.CanMoveChecker(mockBoard, 11, 10, false));
            Assert.False(service.CanMoveChecker(mockBoard, homeBarModel.StartIndexWhite, 1, false));
            Assert.True(service.CanMoveChecker(mockBoard, homeBarModel.StartIndexBlack, 1, false));
            // move black checker from the homebar
            service.MoveChecker(mockBoard, homeBarModel.StartIndexBlack, 1, false);
            Assert.Equal(6, mockBoard.Fields[homeBarModel.StartIndexBlack - 1]);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            // other black checkers can be played again
            Assert.True(service.CanMoveChecker(mockBoard, 14, 7, false));
            Assert.True(service.CanMoveChecker(mockBoard, 13, 8, false));
            Assert.True(service.CanMoveChecker(mockBoard, 12, 9, false));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void MustMoveWhiteCheckerFromHomeIfHit(GameModus gameModus)
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
            mock.As<IHomeBarModel>()
                .Setup(x => x.RemoveFromHomeBar(It.IsAny<bool>(), It.IsAny<int>()))
                .Callback<bool, int>((isWhite, amount) => homeBarModel.RemoveFromHomeBar(isWhite, amount));

            var mockBoard = mock.Object;

            // hit the white checkers and sent them to the home bar
            service.MoveChecker(mockBoard, 15, 7, false);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(1, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, mockBoard.Fields[8]);
            // the hitted white checker should be removed from the field
            Assert.Equal(-14, mockBoard.Fields.Where(f => f < 0).Sum());
            // cannot move all other checkers to empty field 7
            Assert.False(service.CanMoveChecker(mockBoard, 9, 7, true));
            Assert.False(service.CanMoveChecker(mockBoard, 10, 8, true));
            Assert.False(service.CanMoveChecker(mockBoard, 11, 9, true));
            Assert.False(service.CanMoveChecker(mockBoard, 12, 10, true));
            Assert.True(service.CanMoveChecker(mockBoard, homeBarModel.StartIndexWhite, 1, true));
            Assert.False(service.CanMoveChecker(mockBoard, homeBarModel.StartIndexBlack, 1, true));
            // move white checker from the homebar
            service.MoveChecker(mockBoard, homeBarModel.StartIndexWhite, 1, true);
            Assert.Equal(-6, mockBoard.Fields[homeBarModel.StartIndexWhite + 1]);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            // other white checkers can be played again
            Assert.True(service.CanMoveChecker(mockBoard, 9, 7, true));
            Assert.True(service.CanMoveChecker(mockBoard, 10, 8, true));
            Assert.True(service.CanMoveChecker(mockBoard, 11, 9, true));
        }

        #endregion CanMove OnHomeBarCount Tests

        #region Mock Data

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

        #endregion Mock Data
    }
}
