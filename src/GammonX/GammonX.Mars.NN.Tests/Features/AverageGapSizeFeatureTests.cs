using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class AverageGapSizeFeatureTests
    {
        [Fact]
        public void CanEvalPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageGapSizeFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(2, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(2, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageGapSizeFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0.4, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0.16666666666666666, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageGapSizeFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }
    }
}
