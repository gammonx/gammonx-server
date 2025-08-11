using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class PinBoardServiceTests
    {
        #region Simple Interface Tests

        [Theory]
        [InlineData(GameModus.Plakoto)]
        public void HasPinnedCheckersAndCanPin(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var pinModel = boardModel as IPinModel;
            Assert.NotNull(pinModel);
        }

        [Theory]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void HasNoPinnedCheckersAndCanNotPin(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var pinModel = boardModel as IPinModel;
            Assert.Null(pinModel);
        }

        #endregion Simple Interface Tests

        [Fact]
        public void PlakotoBoardWhitePinsBlackAndReleasesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();
            var pinModel = boardModel as IPinModel;
            Assert.NotNull(pinModel);

            // move black checker forwards
            service.MoveCheckerTo(boardModel, 23, 17, false); // Move black checker from point 24 to point 18
            Assert.Equal(14, boardModel.Fields[23]); // Field 24 should now have 14 black pieces
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker

            // check if white checker can pin black checker
            Assert.True(service.CanMoveChecker(boardModel, 0, 17, true));
            service.MoveChecker(boardModel, 0, 17, true);
            // one black checker on field 18 should be pinned now
            Assert.Equal(1, pinModel.PinnedFields[17]);
            // pinned black checker was removed from field 18 and white added
            Assert.Equal(-1, boardModel.Fields[17]);
            // field 18 is blocked for other black checkers
            Assert.False(service.CanMoveChecker(boardModel, 23, 6, false));
            // white can still move to field 18
            Assert.True(service.CanMoveChecker(boardModel, 0, 17, true));

            // release pinned black checker, move white one
            service.MoveChecker(boardModel, 17, 1, true);
            // field 18 is moveable for other black checkers
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));
            // pinned black checker is released and can be moved
            Assert.True(service.CanMoveChecker(boardModel, 17, 2, false));
            Assert.Equal(0, pinModel.PinnedFields[17]);
            // released black checker is back on the field
            Assert.Equal(1, boardModel.Fields[17]);
        }

        [Fact]
        public void PlakotoBoardBlackPinsWhiteAndReleasesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();
            var pinModel = boardModel as IPinModel;
            Assert.NotNull(pinModel);

            // move white checker forwards
            service.MoveCheckerTo(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-14, boardModel.Fields[0]); // Field 0 should now have 14 white pieces
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            // check if white checker can pin black checker
            Assert.True(service.CanMoveChecker(boardModel, 23, 17, false));
            service.MoveChecker(boardModel, 23, 17, false);
            // one white checker on field 7 should be pinned now
            Assert.Equal(-1, pinModel.PinnedFields[6]);
            // pinned white checker was removed from field 7 and black added
            Assert.Equal(1, boardModel.Fields[6]);
            // field 7 is blocked for other white checkers
            Assert.False(service.CanMoveChecker(boardModel, 0, 6, true));
            // black can still move to field 7
            Assert.True(service.CanMoveChecker(boardModel, 23, 17, false));

            // release pinned white checker, move black one
            service.MoveChecker(boardModel, 6, 1, false);
            // field 18 is moveable for other white checkers
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // pinned white checker is released and can be moved
            Assert.True(service.CanMoveChecker(boardModel, 6, 2, true));
            Assert.Equal(0, pinModel.PinnedFields[6]);
            // released black checker is back on the field
            Assert.Equal(-1, boardModel.Fields[6]);
        }
    }
}
