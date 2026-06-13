using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class BlotCountFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new BlotCountFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(3, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(4, blackResult);
        }

        [Fact]
        public void CanEvalPinPlakotoBoard2()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new BlotCountFeature();
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

            var feature = new BlotCountFeature();
            var result = feature.Eval(board, true);
            Assert.Equal(2, result);
            result = feature.Eval(board, false);
            Assert.Equal(1, result);
        }
    }
}
