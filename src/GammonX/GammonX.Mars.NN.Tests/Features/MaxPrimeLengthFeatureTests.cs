using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class MaxPrimeLengthFeatureTests
    {
        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MaxPrimeLengthFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(3, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(4, blackResult);
        }

        [Theory]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Backgammon)]
        public void CanEvalDefaultBoard1(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MaxPrimeLengthFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(3, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(3, blackResult);
        }
    }
}
