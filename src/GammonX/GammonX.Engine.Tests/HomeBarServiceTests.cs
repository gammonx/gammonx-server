using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Engine.Tests.Data;
using GammonX.Engine.Tests.Utils;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Engine.Tests
{
    public class HomeBarServiceTests
    {
        #region Simple Interface Tests

        [Theory]
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
            Assert.True(homeBarModel.CanSendToHomeBar);
            Assert.True(homeBarModel.MustEnterFromHomebar);
        }

        [Theory]
        [InlineData(GameModus.Fevga)]
        public void HasHomeBarAndCannotnHit(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var homeBarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homeBarModel);
            Assert.Equal(-1, homeBarModel.StartIndexWhite);
            Assert.Equal(24, homeBarModel.StartIndexBlack);
            Assert.False(homeBarModel.CanSendToHomeBar);
            Assert.False(homeBarModel.MustEnterFromHomebar);
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
            board.SetFields(BoardMocks.StandardHitBoard);
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            Assert.True(service.CanMoveChecker(board, 8, 7, true));
            Assert.True(service.CanMoveChecker(board, 9, 5, true));
            Assert.False(service.CanMoveChecker(board, 10, 3, true));
            Assert.True(service.CanMoveChecker(board, 11, 1, true));

            // hit the black checkers and sent them to the home bar
            service.MoveChecker(board, 8, 7, true);
            service.MoveChecker(board, 9, 5, true);
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 10, 3, true));
            service.MoveChecker(board, 11, 1, true);

            Assert.Equal(3, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(-1, board.Fields[15]);
            Assert.Equal(-1, board.Fields[14]);
            Assert.Equal(-1, board.Fields[12]);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void BlackCheckerHitsWhite(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var board = service.CreateBoard();
            board.SetFields(BoardMocks.StandardHitBoard);
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            Assert.True(service.CanMoveChecker(board, 15, 7, false));
            Assert.True(service.CanMoveChecker(board, 14, 5, false));
            Assert.False(service.CanMoveChecker(board, 13, 3, false));
            Assert.True(service.CanMoveChecker(board, 12, 1, false));

            // hit the white checkers and sent them to the home bar
            service.MoveChecker(board, 15, 7, false);
            service.MoveChecker(board, 14, 5, false);
            Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 13, 3, false));
            service.MoveChecker(board, 12, 1, false);

            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(3, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, board.Fields[8]);
            Assert.Equal(1, board.Fields[9]);
            Assert.Equal(1, board.Fields[11]);
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
            board.SetFields(BoardMocks.StandardHitBoard);
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            // hit the black checkers and sent them to the home bar
            service.MoveChecker(board, 8, 7, true);
            Assert.Equal(1, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            Assert.Equal(-1, board.Fields[15]);
            // the hitted black checker should be removed from the field
            Assert.Equal(14, board.Fields.Where(f => f > 0).Sum());
            // cannot move all other checkers to empty field 7
            Assert.False(service.CanMoveChecker(board, 14, 7, false));
            Assert.False(service.CanMoveChecker(board, 13, 8, false));
            Assert.False(service.CanMoveChecker(board, 12, 9, false));
            Assert.False(service.CanMoveChecker(board, 11, 10, false));
            Assert.False(service.CanMoveChecker(board, homeBarModel.StartIndexWhite, 1, false));
            Assert.True(service.CanMoveChecker(board, homeBarModel.StartIndexBlack, 1, false));
            // move black checker from the homebar
            service.MoveChecker(board, homeBarModel.StartIndexBlack, 1, false);
            Assert.Equal(6, board.Fields[homeBarModel.StartIndexBlack - 1]);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            // other black checkers can be played again
            Assert.True(service.CanMoveChecker(board, 14, 7, false));
            Assert.True(service.CanMoveChecker(board, 13, 8, false));
            Assert.True(service.CanMoveChecker(board, 12, 9, false));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void MustMoveWhiteCheckerFromHomeIfHit(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var board = service.CreateBoard();
            board.SetFields(BoardMocks.StandardHitBoard);
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            // hit the white checkers and sent them to the home bar
            service.MoveChecker(board, 15, 7, false);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(1, homeBarModel.HomeBarCountWhite);
            Assert.Equal(1, board.Fields[8]);
            // the hitted white checker should be removed from the field
            Assert.Equal(-14, board.Fields.Where(f => f < 0).Sum());
            // cannot move all other checkers to empty field 7
            Assert.False(service.CanMoveChecker(board, 9, 7, true));
            Assert.False(service.CanMoveChecker(board, 10, 8, true));
            Assert.False(service.CanMoveChecker(board, 11, 9, true));
            Assert.False(service.CanMoveChecker(board, 12, 10, true));
            Assert.True(service.CanMoveChecker(board, homeBarModel.StartIndexWhite, 1, true));
            Assert.False(service.CanMoveChecker(board, homeBarModel.StartIndexBlack, 1, true));
            // move white checker from the homebar
            service.MoveChecker(board, homeBarModel.StartIndexWhite, 1, true);
            Assert.Equal(-6, board.Fields[homeBarModel.StartIndexWhite + 1]);
            Assert.Equal(0, homeBarModel.HomeBarCountBlack);
            Assert.Equal(0, homeBarModel.HomeBarCountWhite);
            // other white checkers can be played again
            Assert.True(service.CanMoveChecker(board, 9, 7, true));
            Assert.True(service.CanMoveChecker(board, 10, 8, true));
            Assert.True(service.CanMoveChecker(board, 11, 9, true));
        }

        #endregion CanMove OnHomeBarCount Tests

        #region Bugs

        [Theory]
        [InlineData(GameModus.Fevga)]
        public void CanNotBearOffWhenHomebarIsNotEmpty(GameModus modus)
        {
            // mock board is only applicable for fevga but behavior is same for other boards with homebar
            var service = BoardServiceFactory.Create(modus);
            var board = service.CreateBoard();
            var homeBarModel = board as IHomeBarModel;
            Assert.NotNull(homeBarModel);

            board.SetFields(BoardMocks.FevgaCannotBearOffBoard);
            homeBarModel.AddToHomeBar(false, 13);
            homeBarModel.AddToHomeBar(true, 6);
            board.BearOffChecker(false, 2);

            var canBearOffOperator = board.CanBearOffOperator(false, homeBarModel.StartIndexBlack, 4);
            Assert.False(canBearOffOperator);
            var allInHomeRange = board.AllPiecesInHomeRange(false);
            Assert.False(allInHomeRange);
            var canBearOff = service.CanBearOffChecker(board, homeBarModel.StartIndexBlack, 4, false);
            Assert.False(canBearOff);
            var moveSeq = service.GetLegalMoveSequences(board, false, 4);
            var moves = moveSeq.SelectMany(ms => ms.Moves);
            Assert.All(moves, move => Assert.True(move.From == homeBarModel.StartIndexBlack));
            Assert.All(moves, move => Assert.False(move.To == BoardPositions.BearOffBlack));
        }

        #endregion Bugs
    }
}
