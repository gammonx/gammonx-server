using GammonX.Engine.Services;

using GammonX.Mars.Server.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class MaxPrimeLengthFeatureTests
    {
        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new MaxPrimeLengthFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.Equal(3, whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.Equal(4, blackResult);
        }
    }
}
