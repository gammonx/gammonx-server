using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class BoardServiceTests
    {
        #region Backgammon

        [Fact]
        public void BackgammonBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.Equal(GameModus.Backgammon, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Backgammon, boardModel.Modus);
        }

        [Fact]
        public void BackgammonBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Points.Length);
            Assert.Equal(new Range(18, 23), boardModel.WhiteHome);
            Assert.Equal(new Range(0, 5), boardModel.BlackHome);
            Assert.Equal(0, boardModel.BearOffWhite);
            Assert.Equal(0, boardModel.BearOffBlack);
            var bearOffModel = boardModel as IBearOffBoardModel;
            Assert.NotNull(bearOffModel);
            Assert.Equal(0, bearOffModel.BarWhite);
            Assert.Equal(0, bearOffModel.BarBlack);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.NotNull(doublingCubeModel);
            Assert.Equal(2, doublingCubeModel.DoublingCubeValue);
            Assert.True(doublingCubeModel.DoublingCubeOwner);
            var blockModel = boardModel as IBlockedModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void BackgammonBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Points[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Points[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Points[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Points[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Points[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Points[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Points[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Points[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void BackgammonBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var boardModel = service.CreateBoard();

            service.MovePiece(boardModel, 0, 6, true); // Move white piece from point 1 to point 7
            Assert.Equal(-1, boardModel.Points[0]); // Point 1 should now have 1 white piece
            Assert.Equal(-1, boardModel.Points[6]); // Point 7 should now have 1 white piece

            service.MovePiece(boardModel, 23, 6, false); // Move black piece from point 24 to point 18
            Assert.Equal(1, boardModel.Points[23]); // Point 24 should now have 1 black piece
            Assert.Equal(1, boardModel.Points[17]); // Point 18 should now have 1 black piece
        }

        #endregion Backgammon

        #region Tavli

        [Fact]
        public void PlakotoBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.Equal(GameModus.Plakoto, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Plakoto, boardModel.Modus);
        }

        [Fact]
        public void PlakotoBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Points.Length);
            Assert.Equal(new Range(18, 23), boardModel.WhiteHome);
            Assert.Equal(new Range(0, 5), boardModel.BlackHome);
            Assert.Equal(0, boardModel.BearOffWhite);
            Assert.Equal(0, boardModel.BearOffBlack);
            var bearOffModel = boardModel as IBearOffBoardModel;
            Assert.NotNull(bearOffModel);
            Assert.Equal(0, bearOffModel.BarWhite);
            Assert.Equal(0, bearOffModel.BarBlack);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IBlockedModel;
            Assert.NotNull(blockModel);
            foreach (var point in blockModel.BlockedPoints)
            {
                Assert.Equal(0, point); // There should be no blocked points at game start
            }
        }

        [Fact]
        public void PlakotoBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-15, boardModel.Points[0]); // 15 white pieces on point 1
            Assert.Equal(15, boardModel.Points[23]); // 15 black pieces on point 24
        }

        [Fact]
        public void PlakotoBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();

            service.MovePiece(boardModel, 0, 6, true); // Move white piece from point 1 to point 7
            Assert.Equal(-14, boardModel.Points[0]); // Point 1 should now have 14 white pieces
            Assert.Equal(-1, boardModel.Points[6]); // Point 7 should now have 1 white piece

            service.MovePiece(boardModel, 23, 6, false); // Move black piece from point 24 to point 18
            Assert.Equal(14, boardModel.Points[23]); // Point 24 should now have 14 black pieces
            Assert.Equal(1, boardModel.Points[17]); // Point 18 should now have 1 black piece
        }

        [Fact]
        public void PortesBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.Equal(GameModus.Portes, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Portes, boardModel.Modus);
        }

        [Fact]
        public void PortesBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Points.Length);
            Assert.Equal(new Range(18, 23), boardModel.WhiteHome);
            Assert.Equal(new Range(0, 5), boardModel.BlackHome);
            Assert.Equal(0, boardModel.BearOffWhite);
            Assert.Equal(0, boardModel.BearOffBlack);
            var bearOffModel = boardModel as IBearOffBoardModel;
            Assert.NotNull(bearOffModel);
            Assert.Equal(0, bearOffModel.BarWhite);
            Assert.Equal(0, bearOffModel.BarBlack);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IBlockedModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void PortesBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Points[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Points[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Points[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Points[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Points[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Points[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Points[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Points[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void PortesBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            var boardModel = service.CreateBoard();

            service.MovePiece(boardModel, 0, 6, true); // Move white piece from point 1 to point 7
            Assert.Equal(-1, boardModel.Points[0]); // Point 1 should now have 1 white piece
            Assert.Equal(-1, boardModel.Points[6]); // Point 7 should now have 1 white piece

            service.MovePiece(boardModel, 23, 6, false); // Move black piece from point 24 to point 18
            Assert.Equal(1, boardModel.Points[23]); // Point 24 should now have 1 black piece
            Assert.Equal(1, boardModel.Points[17]); // Point 18 should now have 1 black piece
        }

        [Fact]
        public void FevgaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.Equal(GameModus.Fevga, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Fevga, boardModel.Modus);
        }

        [Fact]
        public void FevgaBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Points.Length);
            Assert.Equal(new Range(18, 23), boardModel.WhiteHome);
            Assert.Equal(new Range(6, 11), boardModel.BlackHome);
            Assert.Equal(0, boardModel.BearOffWhite);
            Assert.Equal(0, boardModel.BearOffBlack);
            var bearOffModel = boardModel as IBearOffBoardModel;
            Assert.NotNull(bearOffModel);
            Assert.Equal(0, bearOffModel.BarWhite);
            Assert.Equal(0, bearOffModel.BarBlack);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
        }

        [Fact]
        public void FevgaBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-15, boardModel.Points[0]); // 15 white pieces on point 1
            Assert.Equal(15, boardModel.Points[12]); // 15 black pieces on point 6
        }

        [Fact]
        public void FevgaBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var boardModel = service.CreateBoard();

            service.MovePiece(boardModel, 0, 6, true); // Move white piece from point 1 to point 7
            Assert.Equal(-14, boardModel.Points[0]); // Point 1 should now have 14 white pieces
            Assert.Equal(-1, boardModel.Points[6]); // Point 7 should now have 1 white piece

            service.MovePiece(boardModel, 12, 6, false); // Move black piece from point 24 to point 18
            Assert.Equal(14, boardModel.Points[12]); // Point 13 should now have 14 black pieces
            Assert.Equal(1, boardModel.Points[18]); // Point 19 should now have 1 black piece

            // move piece around from index 23 to index 1
            service.MovePiece(boardModel, 18, 7, false); // Move black piece from point 19 to point 2
            Assert.Equal(0, boardModel.Points[18]); // Point 18 should now have 0 black piece
            Assert.Equal(1, boardModel.Points[1]); // Point 2 should now have 1 black piece
            
        }

        #endregion

        #region Tavla

        [Fact]
        public void TavlaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.Equal(GameModus.Tavla, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Tavla, boardModel.Modus);
        }

        [Fact]
        public void TavlaBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Points.Length);
            Assert.Equal(new Range(18, 23), boardModel.WhiteHome);
            Assert.Equal(new Range(0, 5), boardModel.BlackHome);
            Assert.Equal(0, boardModel.BearOffWhite);
            Assert.Equal(0, boardModel.BearOffBlack);
            var bearOffModel = boardModel as IBearOffBoardModel;
            Assert.NotNull(bearOffModel);
            Assert.Equal(0, bearOffModel.BarWhite);
            Assert.Equal(0, bearOffModel.BarBlack);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IBlockedModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void TavlaBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Points[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Points[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Points[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Points[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Points[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Points[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Points[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Points[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void TavlaBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            var boardModel = service.CreateBoard();

            service.MovePiece(boardModel, 0, 6, true); // Move white piece from point 1 to point 7
            Assert.Equal(-1, boardModel.Points[0]); // Point 1 should now have 1 white piece
            Assert.Equal(-1, boardModel.Points[6]); // Point 7 should now have 1 white piece

            service.MovePiece(boardModel, 23, 6, false); // Move black piece from point 24 to point 18
            Assert.Equal(1, boardModel.Points[23]); // Point 24 should now have 1 black piece
            Assert.Equal(1, boardModel.Points[17]); // Point 18 should now have 1 black piece
        }

        #endregion Tavla
    }
}
