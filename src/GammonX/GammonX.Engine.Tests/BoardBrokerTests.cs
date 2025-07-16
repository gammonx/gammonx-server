using GammonX.Engine.Models;
using GammonX.Engine.Services;
using Moq;

namespace GammonX.Engine.Tests
{
    public class BoardBrokerTests
    {
        [Fact]
        public void CheckersAreNotInHomeRangeOnInitialBoard()
        {
            var bgService = BoardServiceFactory.Create(GameModus.Backgammon);
            var bgBoard = bgService.CreateBoard();
            Assert.False(bgBoard.AllPiecesInHomeRange(true));
            Assert.False(bgBoard.AllPiecesInHomeRange(false));

            var tavlaService = BoardServiceFactory.Create(GameModus.Tavla);
            var tavlaBoard = tavlaService.CreateBoard();
            Assert.False(tavlaBoard.AllPiecesInHomeRange(true));
            Assert.False(tavlaBoard.AllPiecesInHomeRange(false));

            var portesService = BoardServiceFactory.Create(GameModus.Portes);
            var portesBoard = portesService.CreateBoard();
            Assert.False(portesBoard.AllPiecesInHomeRange(true));
            Assert.False(portesBoard.AllPiecesInHomeRange(false));

            var plakotorService = BoardServiceFactory.Create(GameModus.Plakoto);
            var plakotoBoard = plakotorService.CreateBoard();
            Assert.False(plakotoBoard.AllPiecesInHomeRange(true));
            Assert.False(plakotoBoard.AllPiecesInHomeRange(false));

            var fevgaService = BoardServiceFactory.Create(GameModus.Fevga);
            var fevgaBoard = fevgaService.CreateBoard();
            Assert.False(fevgaBoard.AllPiecesInHomeRange(true));
            Assert.False(fevgaBoard.AllPiecesInHomeRange(false));
        }

        [Fact]
        public void BackgammonAreInHomeRange()
        {
            // backgammon 
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var board = service.CreateBoard();

            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            var mockBoard = mock.Object;
            Assert.True(mockBoard.AllPiecesInHomeRange(true));
            Assert.True(mockBoard.AllPiecesInHomeRange(false));
            // tavla 
            service = BoardServiceFactory.Create(GameModus.Tavla);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.True(mockBoard.AllPiecesInHomeRange(true));
            Assert.True(mockBoard.AllPiecesInHomeRange(false));
            // portes 
            service = BoardServiceFactory.Create(GameModus.Portes);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.True(mockBoard.AllPiecesInHomeRange(true));
            Assert.True(mockBoard.AllPiecesInHomeRange(false));
            // plakoto 
            service = BoardServiceFactory.Create(GameModus.Plakoto);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.True(mockBoard.AllPiecesInHomeRange(true));
            Assert.True(mockBoard.AllPiecesInHomeRange(false));
        }

        [Fact]
        public void FevgaAreInHomeRange()
        {
            // fevga
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(FevgaCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);
            var mockBoard = mock.Object;
            Assert.True(mockBoard.AllPiecesInHomeRange(true));
            Assert.True(mockBoard.AllPiecesInHomeRange(false));
        }

        [Fact]
        public void BackgammonAreNotInHomeRange()
        {
            // backgammon
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var board = service.CreateBoard();

            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanNotBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            var mockBoard = mock.Object;
            Assert.False(mockBoard.AllPiecesInHomeRange(true));
            Assert.False(mockBoard.AllPiecesInHomeRange(false));
            // tavla
            service = BoardServiceFactory.Create(GameModus.Tavla);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanNotBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.False(mockBoard.AllPiecesInHomeRange(true));
            Assert.False(mockBoard.AllPiecesInHomeRange(false));
            // portes
            service = BoardServiceFactory.Create(GameModus.Portes);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanNotBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.False(mockBoard.AllPiecesInHomeRange(true));
            Assert.False(mockBoard.AllPiecesInHomeRange(false));
            // plakoto
            service = BoardServiceFactory.Create(GameModus.Plakoto);
            board = service.CreateBoard();

            mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanNotBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);

            mockBoard = mock.Object;
            Assert.False(mockBoard.AllPiecesInHomeRange(true));
            Assert.False(mockBoard.AllPiecesInHomeRange(false));
        }

        [Fact]
        public void FevgaAreNotInHomeRange()
        {
            // fevga
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(FevgaCanNotBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);
            var mockBoard = mock.Object;
            Assert.False(mockBoard.AllPiecesInHomeRange(true));
            Assert.False(mockBoard.AllPiecesInHomeRange(false));
        }

        [Fact]
        public void BackgammonCanMove()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var board = service.CreateBoard();
            // open field
            Assert.True(board.CanMove(0, 1, true));
            // blocked by opponent black checkers
            Assert.False(board.CanMove(0, 5, true));
            Assert.False(board.CanMove(0, 7, true));
            // open field
            Assert.True(board.CanMove(23, 22, false));
            // blocked by opponent white checkers
            Assert.False(board.CanMove(23, 18, true));
            Assert.False(board.CanMove(23, 16, true));
            // empty from field
            Assert.False(board.CanMove(1, 2, true));
            Assert.False(board.CanMove(22, 21, false));
            // invalid indices
            Assert.False(board.CanMove(-1, 24, true));
            Assert.False(board.CanMove(24, -1, false));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Plakoto)]
        public void BackGammonCanBearOff(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var board = service.CreateBoard();

            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(StandardCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.MoveOperator).Returns(board.MoveOperator);
            mock.SetupGet(b => b.CanBearOffOperator).Returns(board.CanBearOffOperator);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);
            var mockBoard = mock.Object;

            // can bear off black
            Assert.True(mockBoard.CanBearOff(0, 1, false));
            Assert.True(mockBoard.CanBearOff(1, 2, false));
            Assert.True(mockBoard.CanBearOff(2, 3, false));
            Assert.True(mockBoard.CanBearOff(3, 4, false));
            Assert.True(mockBoard.CanBearOff(4, 5, false));
            Assert.True(mockBoard.CanBearOff(5, 6, false));
            // cannot bear off black
            Assert.False(mockBoard.CanBearOff(1, 1, false));
            Assert.False(mockBoard.CanBearOff(2, 2, false));
            Assert.False(mockBoard.CanBearOff(3, 3, false));
            Assert.False(mockBoard.CanBearOff(4, 4, false));
            Assert.False(mockBoard.CanBearOff(5, 5, false));
            // can bear off white
            Assert.True(mockBoard.CanBearOff(23, 1, true));
            Assert.True(mockBoard.CanBearOff(22, 2, true));
            Assert.True(mockBoard.CanBearOff(21, 3, true));
            Assert.True(mockBoard.CanBearOff(20, 4, true));
            Assert.True(mockBoard.CanBearOff(19, 5, true));
            Assert.True(mockBoard.CanBearOff(18, 6, true));
            // cannot bear off white
            Assert.False(mockBoard.CanBearOff(22, 1, true));
            Assert.False(mockBoard.CanBearOff(21, 2, true));
            Assert.False(mockBoard.CanBearOff(20, 3, true));
            Assert.False(mockBoard.CanBearOff(19, 4, true));
            Assert.False(mockBoard.CanBearOff(18, 5, true));
        }

        [Fact]
        public void FevgaCanBearOff()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();

            var mock = new Mock<IBoardModel>();
            mock.SetupGet(b => b.Fields).Returns(FevgaCanBearOffBoard);
            mock.SetupGet(b => b.HomeRangeBlack).Returns(board.HomeRangeBlack);
            mock.SetupGet(b => b.HomeRangeWhite).Returns(board.HomeRangeWhite);
            mock.SetupGet(b => b.MoveOperator).Returns(board.MoveOperator);
            mock.SetupGet(b => b.CanBearOffOperator).Returns(board.CanBearOffOperator);
            mock.SetupGet(b => b.IsInHomeOperator).Returns(board.IsInHomeOperator);
            var mockBoard = mock.Object;

            // can bear off black
            Assert.True(mockBoard.CanBearOff(11, 1, false));
            Assert.True(mockBoard.CanBearOff(10, 2, false));
            Assert.True(mockBoard.CanBearOff(9, 3, false));
            Assert.True(mockBoard.CanBearOff(8, 4, false));
            Assert.True(mockBoard.CanBearOff(7, 5, false));
            Assert.True(mockBoard.CanBearOff(6, 6, false));
            // cannot bear off black
            Assert.False(mockBoard.CanBearOff(10, 1, false));
            Assert.False(mockBoard.CanBearOff(9, 2, false));
            Assert.False(mockBoard.CanBearOff(8, 3, false));
            Assert.False(mockBoard.CanBearOff(7, 4, false));
            Assert.False(mockBoard.CanBearOff(6, 5, false));
            // can bear off white
            Assert.True(mockBoard.CanBearOff(23, 1, true));
            Assert.True(mockBoard.CanBearOff(22, 2, true));
            Assert.True(mockBoard.CanBearOff(21, 3, true));
            Assert.True(mockBoard.CanBearOff(20, 4, true));
            Assert.True(mockBoard.CanBearOff(19, 5, true));
            Assert.True(mockBoard.CanBearOff(18, 6, true));
            // cannot bear off white
            Assert.False(mockBoard.CanBearOff(22, 1, true));
            Assert.False(mockBoard.CanBearOff(21, 2, true));
            Assert.False(mockBoard.CanBearOff(20, 3, true));
            Assert.False(mockBoard.CanBearOff(19, 4, true));
            Assert.False(mockBoard.CanBearOff(18, 5, true));
        }

        [Fact]
        public void WhitePlayerWithPiecesOnHomeBarReturnsTrue()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.HomeBarCountWhite)
                .Returns(2);
            var board = mock.Object;
            Assert.True(board.MustEnterFromHomeBar(true));
        }

        [Fact]
        public void WhitePlayerNoPiecesOnHomeBarReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.HomeBarCountWhite)
                .Returns(0);
            var board = mock.Object;
            Assert.False(board.MustEnterFromHomeBar(true));
        }

        [Fact]
        public void BlackPlayerWithPiecesOnHomeBarReturnsTrue()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.HomeBarCountBlack)
                .Returns(1);
            var board = mock.Object;
            Assert.True(board.MustEnterFromHomeBar(false));
        }

        [Fact]
        public void BlackPlayerNoPiecesOnHomeBarReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.HomeBarCountBlack)
                .Returns(0);
            var board = mock.Object;
            Assert.False(board.MustEnterFromHomeBar(false));
        }

        [Fact]
        public void ModelDoesNotImplementIHomeBarModelReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            var board = mock.Object;
            Assert.False(board.MustEnterFromHomeBar(true));
            Assert.False(board.MustEnterFromHomeBar(false));
        }

        [Fact]
        public void WhitePlayerWithCorrectStartIndexReturnsTrue()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.StartIndexWhite)
                .Returns(-1);
            var board = mock.Object;
            var result = board.EntersFromHomeBar(-1, true);
            Assert.True(result);
        }

        [Fact]
        public void WhitePlayerWithWrongStartIndexReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.StartIndexWhite)
                .Returns(-1);
            var board = mock.Object;
            var result = board.EntersFromHomeBar(0, true);
            Assert.False(result);
        }

        [Fact]
        public void BlackPlayerWithCorrectStartIndexReturnsTrue()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.StartIndexBlack)
                .Returns(24);
            var board = mock.Object;
            var result = board.EntersFromHomeBar(24, false);
            Assert.True(result);
        }

        [Fact]
        public void BlackPlayerWithWrongStartIndexReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            mock.As<IHomeBarModel>()
                .SetupGet(x => x.StartIndexBlack)
                .Returns(24);
            var board = mock.Object;
            var result = board.EntersFromHomeBar(10, false);
            Assert.False(result);
        }

        [Fact]
        public void WhitePlayerWithoutHomeBarModelReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            var board = mock.Object;
            var result = board.EntersFromHomeBar(-1, true);
            Assert.False(result);
        }

        [Fact]
        public void BlackPlayerWithoutHomeBarModelReturnsFalse()
        {
            var mock = new Mock<IBoardModel>();
            var board = mock.Object;
            var result = board.EntersFromHomeBar(24, false);
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1, 5, true)]  // from out of bounds
        [InlineData(0, 24, true)]  // to out of bounds
        [InlineData(-5, -1, true)] // both invalid
        public void IndexOutOfBoundsReturnsFalse(int from, int to, bool isWhite)
        {
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(m => m.Fields).Returns(new int[24]);
            var board = mock.Object;
            var result = board.CanMove(from, to, isWhite);
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1, 5, true)]
        [InlineData(24, 5, false)]
        public void FromIsHomeBarAndToIsValidReturnsTrue(int from, int to, bool isWhite)
        {
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(m => m.Fields).Returns(new int[24]);
            mock.SetupGet(b => b.BlockAmount).Returns(2);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexWhite).Returns(-1);
            mock.As<IHomeBarModel>().SetupGet(b => b.StartIndexBlack).Returns(24);
            var board = mock.Object;
            var result = board.CanMove(from, to, isWhite);
            Assert.True(result);
        }

        [Theory]
        [InlineData(5, 6, true, 2)]   // white: 2 black pieces on from
        [InlineData(5, 6, false, -2)] // black: 2 white pieces on from
        public void MoveFromEmptyFieldReturnsFalse(int from, int to, bool isWhite, int fromValue)
        {
            var fields = new int[24];
            fields[from] = fromValue;
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(m => m.Fields).Returns(fields);
            var board = mock.Object;
            var result = board.CanMove(from, to, isWhite);
            Assert.False(result);
        }

        [Theory]
        [InlineData(true, 2)]  // white blocked by 2 black pieces
        [InlineData(false, -2)] // black blocked by 2 white pieces
        public void BlockedByOpponentReturnsFalse(bool isWhite, int toPointValue)
        {
            var fields = new int[24];
            fields[5] = isWhite ? -1 : 1; // valid piece
            fields[6] = toPointValue;
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(m => m.Fields).Returns(fields);
            mock.Setup(m => m.BlockAmount).Returns(2);
            var board = mock.Object;
            var result = board.CanMove(5, 6, isWhite);
            Assert.False(result);
        }

        [Theory]
        [InlineData(true, 2, 1)]  // white: 2 black on target, but 1 pinned
        [InlineData(false, -2, -1)] // black: 2 white on target, but 1 pinned
        public void BlockedButPinnedReturnsFalse(bool isWhite, int toValue, int pinned)
        {
            var fields = new int[24];
            fields[5] = isWhite ? -1 : 1; // has checker to move
            fields[6] = toValue;

            var mock = new Mock<IBoardModel>();
            var pinnedFields = new int[24];
            pinnedFields[6] = pinned;
            mock.As<IPinModel>().SetupGet(m => m.PinnedFields).Returns(pinnedFields);
            mock.SetupGet(m => m.Fields).Returns(fields);
            mock.Setup(m => m.BlockAmount).Returns(2);
            var board = mock.Object;
            var result = board.CanMove(5, 6, isWhite);
            Assert.False(result);
        }

        [Theory]
        [InlineData(true, -1)] // white moving to empty field
        [InlineData(false, 1)] // black moving to empty field
        public void ValidMoveReturnsTrue(bool isWhite, int fromValue)
        {
            var fields = new int[24];
            fields[5] = fromValue; // valid piece
            fields[6] = 0;         // target empty
            var mock = new Mock<IBoardModel>();
            mock.SetupGet(m => m.Fields).Returns(fields);
            mock.Setup(m => m.BlockAmount).Returns(2);
            var board = mock.Object;
            var result = board.CanMove(5, 6, isWhite);
            Assert.True(result);
        }

        #region Board Mocks

        private readonly int[] StandardCanBearOffBoard = new int[24]
        {
             5, // 0 – Black Home
             5, // 1 – Black Home
             3, // 2 – Black Home
             1, // 3 - Black Home
             1, // 4 - Black Home
             1, // 5 - Black Home
             0, // 6
             0, // 7
             0, // 8
             0, // 9
             0, // 10
             0, // 11
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -3, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

        private readonly int[] StandardCanNotBearOffBoard = new int[24]
        {
             4, // 0 – Black Home
             5, // 1 – Black Home
             3, // 2 – Black Home
             1, // 3 - Black Home
             1, // 4 - Black Home
             1, // 5 - Black Home
             1, // 6
             0, // 7
             0, // 8
             0, // 9
             0, // 10
             0, // 11
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
            -1, // 17
            -2, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

        private readonly int[] FevgaCanBearOffBoard = new int[24]
        {
             0, // 0
             0, // 1
             0, // 2
             0, // 3
             0, // 4
             0, // 5
             5, // 6  – Black Home
             5, // 7  – Black Home
             2, // 8  – Black Home
             1, // 9  – Black Home
             1, // 10 – Black Home
             1, // 11 – Black Home
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -3, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

        private readonly int[] FevgaCanNotBearOffBoard = new int[24]
        {
             1, // 0
            -1, // 1
             0, // 2
             0, // 3
             0, // 4
             0, // 5
             4, // 6  – Black Home
             5, // 7  – Black Home
             2, // 8  – Black Home
             1, // 9  – Black Home
             1, // 10 – Black Home
             1, // 11 – Black Home
             0, // 12
             0, // 13
             0, // 14
             0, // 15
             0, // 16
             0, // 17
            -2, // 18 – White Home
            -3, // 19 – White Home
            -3, // 20 – White Home
            -3, // 21 – White Home
            -2, // 22 – White Home
            -1  // 23 - White Home
        };

        #endregion Board Mocks
    }
}
