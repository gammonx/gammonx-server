using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
{
    public class PrimeProbabilityFeatureTests
    {
        [Fact]
        public void CanEvalFevgaBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new PrimeProbabilityFeature(boardService);
            var result = feature.Eval(board, true);
            Assert.Equal(0.83333333333333337, result.PrimeProbabilityPlayer);
            Assert.Equal(0.77777777777777779, result.PrimeProbabilityOpp);
            result = feature.Eval(board, false);
            Assert.Equal(0.83333333333333337, result.PrimeProbabilityOpp);
            Assert.Equal(0.77777777777777779, result.PrimeProbabilityPlayer);
        }
    }
}
