using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class AnchorCountFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new AnchorCountFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(2, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(2, blackResult);
        }
    }
}
