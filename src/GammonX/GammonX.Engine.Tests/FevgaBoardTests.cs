using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Engine.Tests
{
    public class FevgaBoardTests
    {
        [Fact]
        public void FevgaWhiteHasToPassBlackStart()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);

			Assert.Equal(-1, board.Fields[0]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, homebarModel.StartIndexWhite, 1, true));
            // move first white opening checker
            service.MoveChecker(board, homebarModel.StartIndexWhite, 1, true);
            // cannot move any other white checker from start
            Assert.False(service.CanMoveChecker(board, homebarModel.StartIndexWhite, 1, true));
            Assert.Equal(-2, board.Fields[0]);

			// we move one by one until the black start is passed

			Assert.True(service.CanMoveChecker(board, 0, 1, true));
			service.MoveChecker(board, 0, 1, true);
			Assert.False(service.CanMoveChecker(board, -1, 1, true));
			Assert.Equal(-1, board.Fields[0]);
			Assert.Equal(-1, board.Fields[1]);
			Assert.Equal(-1, board.Fields[0]);

			Assert.True(service.CanMoveChecker(board, 1, 1, true));
            service.MoveChecker(board, 1, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[1]);
            Assert.Equal(-1, board.Fields[2]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 2, 1, true));
            service.MoveChecker(board, 2, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[2]);
            Assert.Equal(-1, board.Fields[3]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 3, 1, true));
            service.MoveChecker(board, 3, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[3]);
            Assert.Equal(-1, board.Fields[4]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 4, 1, true));
            service.MoveChecker(board, 4, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[4]);
            Assert.Equal(-1, board.Fields[5]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 5, 1, true));
            service.MoveChecker(board, 5, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[5]);
            Assert.Equal(-1, board.Fields[6]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 6, 1, true));
            service.MoveChecker(board, 6, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[6]);
            Assert.Equal(-1, board.Fields[7]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 7, 1, true));
            service.MoveChecker(board, 7, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[7]);
            Assert.Equal(-1, board.Fields[8]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 8, 1, true));
            service.MoveChecker(board, 8, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[8]);
            Assert.Equal(-1, board.Fields[9]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 9, 1, true));
            service.MoveChecker(board, 9, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[9]);
            Assert.Equal(-1, board.Fields[10]);
            Assert.Equal(-1, board.Fields[0]);

            Assert.True(service.CanMoveChecker(board, 10, 1, true));
            service.MoveChecker(board, 10, 1, true);
            Assert.False(service.CanMoveChecker(board, 0, 1, true));
            Assert.Equal(0, board.Fields[10]);
            Assert.Equal(-1, board.Fields[11]);
            Assert.Equal(-1, board.Fields[0]);

            // we are now at field 12 (index 11)
            // next field is black start
            Assert.False(service.CanMoveChecker(board, 11, 1, true));
            // we have to move 2
            Assert.True(service.CanMoveChecker(board, 11, 2, true));
            service.MoveChecker(board, 11, 2, true);
            Assert.Equal(-1, board.Fields[13]);
            Assert.Equal(-1, board.Fields[0]);
            // now we can move any other white checker
            Assert.True(service.CanMoveChecker(board, 0, 1, true));
            service.MoveChecker(board, 0, 1, true);
            Assert.Equal(-0, board.Fields[0]);
            Assert.Equal(-1, board.Fields[1]);

			// we move a second checker from the homebar
			Assert.True(service.CanMoveChecker(board, homebarModel.StartIndexWhite, 1, true));
			service.MoveChecker(board, homebarModel.StartIndexWhite, 1, true);
			Assert.Equal(12, homebarModel.HomeBarCountWhite);
		}

        [Fact]
        public void FevgaBlackHasToPassWhiteStart()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
            var homebarModel = board as IHomeBarModel;
            Assert.NotNull(homebarModel);

			Assert.Equal(-1, board.Fields[0]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, homebarModel.StartIndexBlack, 1, false));
            // move first black opening checker
            service.MoveChecker(board, homebarModel.StartIndexBlack, 1, false);
            Assert.Equal(13, homebarModel.HomeBarCountBlack);
            // cannot move any other black checker from start
            Assert.False(service.CanMoveChecker(board, homebarModel.StartIndexBlack, 1, false));
            Assert.Equal(2, board.Fields[12]);

			// we move one by one until the white start is passed

			Assert.True(service.CanMoveChecker(board, 12, 1, false));
			service.MoveChecker(board, 12, 1, false);
			Assert.False(service.CanMoveChecker(board, 11, 1, false));
			Assert.Equal(1, board.Fields[12]);
			Assert.Equal(1, board.Fields[13]);
			Assert.Equal(1, board.Fields[12]);

			Assert.True(service.CanMoveChecker(board, 13, 1, false));
			service.MoveChecker(board, 13, 1, false);
			Assert.False(service.CanMoveChecker(board, 12, 1, false));
			Assert.Equal(0, board.Fields[13]);
            Assert.Equal(1, board.Fields[14]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 14, 1, false));
            service.MoveChecker(board, 14, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[14]);
            Assert.Equal(1, board.Fields[15]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 15, 1, false));
            service.MoveChecker(board, 15, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[15]);
            Assert.Equal(1, board.Fields[16]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 16, 1, false));
            service.MoveChecker(board, 16, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[16]);
            Assert.Equal(1, board.Fields[17]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 17, 1, false));
            service.MoveChecker(board, 17, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[17]);
            Assert.Equal(1, board.Fields[18]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 18, 1, false));
            service.MoveChecker(board, 18, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[18]);
            Assert.Equal(1, board.Fields[19]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 19, 20, false));
            service.MoveChecker(board, 19, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[19]);
            Assert.Equal(1, board.Fields[20]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 20, 1, false));
            service.MoveChecker(board, 20, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[20]);
            Assert.Equal(1, board.Fields[21]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 21, 1, false));
            service.MoveChecker(board, 21, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[21]);
            Assert.Equal(1, board.Fields[22]);
            Assert.Equal(1, board.Fields[12]);

            Assert.True(service.CanMoveChecker(board, 22, 1, false));
            service.MoveChecker(board, 22, 1, false);
            Assert.False(service.CanMoveChecker(board, 12, 1, false));
            Assert.Equal(0, board.Fields[22]);
            Assert.Equal(1, board.Fields[23]);
            Assert.Equal(1, board.Fields[12]);

            // we are now at field 24 (index 23)
            // next field is white start
            Assert.False(service.CanMoveChecker(board, 23, 1, false));
            // we have to move 2
            Assert.True(service.CanMoveChecker(board, 23, 2, false));
            service.MoveChecker(board, 23, 2, false);
            Assert.Equal(1, board.Fields[1]); // we move around from index 23 to index 1
            Assert.Equal(1, board.Fields[12]);
            // now we can move any other black checker
            Assert.True(service.CanMoveChecker(board, 12, 1, false));
            service.MoveChecker(board, 12, 1, false);
            Assert.Equal(0, board.Fields[12]);
            Assert.Equal(1, board.Fields[13]);

            // we move a second checker from the homebar
			Assert.True(service.CanMoveChecker(board, homebarModel.StartIndexBlack, 1, false));
			service.MoveChecker(board, homebarModel.StartIndexBlack, 1, false);
			Assert.Equal(12, homebarModel.HomeBarCountBlack);
		}

        [Fact]
        public void FevgaBlackIsBlockedByASingleWhiteChecker()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);
			service.MoveChecker(board, homebarModel.StartIndexWhite, 14, true);
            Assert.False(service.CanMoveChecker(board, homebarModel.StartIndexBlack, 2, false));
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, homebarModel.StartIndexBlack, 2, false));
        }

        [Fact]
        public void FevgaWhiteIsBlockedByASingleBlackChecker()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var board = service.CreateBoard();
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);
			service.MoveChecker(board, homebarModel.StartIndexBlack, 14, false);
            Assert.False(service.CanMoveChecker(board, homebarModel.StartIndexWhite, 2, true));
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, homebarModel.StartIndexWhite, 2, true));
        }

        [Fact]
        public void FevgaRecoverRollBasedOnFromToMove()
        {
			var service = BoardServiceFactory.Create(GameModus.Fevga);
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
            roll = board.RecoverRollOperator(false, 18, 0);
            Assert.Equal(6, roll);
            roll = board.RecoverRollOperator(false, 5, 11);
            Assert.Equal(6, roll);
            roll = board.RecoverRollOperator(false, BoardPositions.HomeBarBlack, 17);
            Assert.Equal(6, roll);
            roll = board.RecoverRollOperator(false, 6, BoardPositions.BearOffBlack);
            Assert.Equal(6, roll);
		}
    }
}
