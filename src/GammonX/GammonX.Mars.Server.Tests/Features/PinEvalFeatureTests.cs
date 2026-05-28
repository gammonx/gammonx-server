using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class PinEvalFeatureTests
    {
        [Fact]
        public void CanEvalPinPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PinPlakotoBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new PinEvalFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(3, whiteResult.PinnedPlayerCount);
            Assert.Equal(4, whiteResult.PinnedOppCount);
            Assert.Equal(1, whiteResult.OppMotherPinned);
            Assert.Equal(1, whiteResult.PlayerMotherPinned);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(4, blackResult.PinnedPlayerCount);
            Assert.Equal(3, blackResult.PinnedOppCount);
            Assert.Equal(1, blackResult.OppMotherPinned);
            Assert.Equal(1, blackResult.PlayerMotherPinned);
        }
    }
}
