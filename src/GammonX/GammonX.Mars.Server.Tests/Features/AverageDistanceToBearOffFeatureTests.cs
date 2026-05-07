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
            Assert.Equal(20, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(19, blackResult);
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
            Assert.Equal(21, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(20, blackResult);
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
            Assert.Equal(23, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }
    }
}
