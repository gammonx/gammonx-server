using GammonX.Engine.Services;
using GammonX.Mars.Server.Features;
using GammonX.Mars.Server.Tests.Data;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class ContactProbabilityFeatureTests
    {
        [Fact]
        public void CanEvalStartPlakotoBoard()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var board = boardService.CreateBoard();

            var feature = new ContactProbabilityFeature(boardService);
            var result = feature.Eval(board, true);

            Assert.Equal(0.0, result.HitProbability1);
            Assert.Equal(0.0, result.HitProbability2);
            Assert.Equal(0.75, result.EscapeProbability1);
            Assert.Equal(0.1111111111111111, result.EscapeProbability2);
        }

        [Fact]
        public void CanEvalPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new ContactProbabilityFeature(boardService);
            var result = feature.Eval(board, true);

            Assert.Equal(0.083333333333333329, result.HitProbability1);
            Assert.Equal(0.0, result.HitProbability2);
            Assert.Equal(1.0, result.EscapeProbability1);
            Assert.Equal(0.3611111111111111, result.EscapeProbability2);
        }

        [Fact]
        public void CanEvalPlakotoBoard2()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard2);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new ContactProbabilityFeature(boardService);
            var result = feature.Eval(board, true);

            Assert.Equal(0.16666666666666666, result.HitProbability1);
            Assert.Equal(0.027777777777777776, result.HitProbability2);
            Assert.Equal(1.0, result.EscapeProbability1);
            Assert.Equal(0.3611111111111111, result.EscapeProbability2);
        }
    }
}
