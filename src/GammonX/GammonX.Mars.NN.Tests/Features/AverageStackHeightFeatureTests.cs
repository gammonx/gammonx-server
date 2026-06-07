using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class AverageStackHeightFeatureTests
    {
        [Fact]
        public void CanEvalPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageStackHeightFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(5, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(5, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageStackHeightFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(1.6666666666666667, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(2.142857142857142857142857142857, blackResult);
        }

        [Fact]
        public void CanEvalPlakotoBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageStackHeightFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(15, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }
    }
}
