using GammonX.Engine.Services;

using GammonX.Mars.Server.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class BlotInStartRangeCountFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new BlotInStartRangeCountFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(1, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(2, blackResult);
        }

        [Fact]
        public void CanEvalPinPlakotoBoard2()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new BlotInStartRangeCountFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(1, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(1, blackResult);
        }
    }
}
