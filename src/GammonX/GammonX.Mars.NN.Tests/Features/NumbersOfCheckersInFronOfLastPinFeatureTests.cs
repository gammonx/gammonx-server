using GammonX.Engine.Services;
using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Tests.Data;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class NumbersOfCheckersInFronOfLastPinFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new NumbersOfCheckersInFronOfLastPinFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(12, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(12, blackResult);
        }
    }
}
