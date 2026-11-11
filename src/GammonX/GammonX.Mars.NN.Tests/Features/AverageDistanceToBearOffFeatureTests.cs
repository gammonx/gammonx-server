using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class AverageDistanceToBearOffFeatureTests
    {
        [Fact]
        public void CanEvalPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageDistanceToBearOffFeature();
            var whiteResult = feature.Eval(board, true);
            // Each side has 13 checkers at distance 24, one at 20, and one at 18.
            Assert.Equal(350d / 15, whiteResult, 10);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(350d / 15, blackResult, 10);
        }

        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageDistanceToBearOffFeature();
            var whiteResult = feature.Eval(board, true);
            // White's visible checkers contribute 207 pips; five reserves contribute 125.
            Assert.Equal(332d / 15, whiteResult, 10);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(304d / 15, blackResult, 10);
        }

        [Fact]
        public void CanEvalFevgaBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AverageDistanceToBearOffFeature();
            var whiteResult = feature.Eval(board, true);
            // All fifteen white checkers remain in reserve, 25 pips from bearing off.
            Assert.Equal(25.0, whiteResult, 2);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0.0, blackResult, 2);
        }
    }
}
