using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class MotherDistanceFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MotherDistanceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(18, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(18, blackResult);
        }

        [Fact]
        public void CanEvalPlakotoBorneOffBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBorneOffBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MotherDistanceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }

        [Fact]
        public void CanEvalPlakotoBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MotherDistanceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(18, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }
    }
}
