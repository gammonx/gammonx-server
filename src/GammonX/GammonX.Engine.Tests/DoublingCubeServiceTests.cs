using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class DoublingCubeServiceTests
    {
        #region Simple Interface Tests

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public void HasDoublingCubeCapability(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.NotNull(doublingCubeModel);
        }

        [Theory]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void HasNoDoublingCubeCapability(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
        }

        #endregion Simple Interface Tests
    }
}
