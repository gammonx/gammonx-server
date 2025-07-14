using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class BoardServiceTests
    {
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
            Assert.Equal(23, boardModel.WhiteHome);
            Assert.Equal(0, boardModel.BlackHome);
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
        public void PlakotoBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.Equal(GameModus.Plakoto, service.Modus);
        }

        [Fact]
        public void PortesBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.Equal(GameModus.Portes, service.Modus);
        }

        [Fact]
        public void FevgaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.Equal(GameModus.Fevga, service.Modus);
        }

        [Fact]
        public void TavlaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.Equal(GameModus.Tavla, service.Modus);
        }
    }
}
