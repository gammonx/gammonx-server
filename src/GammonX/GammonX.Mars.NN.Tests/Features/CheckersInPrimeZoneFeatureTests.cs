using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class CheckersInPrimeZoneFeatureTests
    {
        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new CheckersInPrimeZoneFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard2()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new CheckersInPrimeZoneFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(1, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(1, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBoard3()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard3);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new CheckersInPrimeZoneFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(4, blackResult);
        }

        [Fact]
        public void CanEvalFevgaBlackWonBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBlackWonBoard);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new CheckersInPrimeZoneFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(0, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(0, blackResult);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        public void CanEvalDefaultBoard1(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var feature = new CheckersInPrimeZoneFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(8, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(8, blackResult);
        }
    }
}
