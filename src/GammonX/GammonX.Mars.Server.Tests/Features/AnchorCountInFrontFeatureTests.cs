using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class AnchorCountInFrontFeatureTests
    {
        [Fact]
        public void CanEvalFevgaBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AnchorCountInFrontFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AnchorCountInFrontFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(6, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(7, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard2()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AnchorCountInFrontFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(4, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(7, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard3()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard3);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AnchorCountInFrontFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(2, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(2, blackResult);
        }
    }
}
