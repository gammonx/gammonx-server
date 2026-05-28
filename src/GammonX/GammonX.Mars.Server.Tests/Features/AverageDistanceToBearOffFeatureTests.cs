using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
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
            Assert.Equal(22.33, whiteResult, 2);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(22.33, blackResult, 2);
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
            Assert.Equal(20.80, whiteResult, 2);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(19.27, blackResult, 2);
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
            Assert.Equal(23.0, whiteResult, 2);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0.0, blackResult, 2);
        }
    }
}
