using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Features
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

            Assert.Equal(0.0, result.Player.HitProbability1);
            Assert.Equal(0.0, result.Player.HitProbability2);
            Assert.Equal(0.75, result.Player.EscapeProbability1);
            Assert.Equal(0.1111111111111111, result.Player.EscapeProbability2);
        }

        [Fact]
        public void EvalForBlackAndWhiteEquals()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var board = boardService.CreateBoard();

            var feature = new ContactProbabilityFeature(boardService);
            var result = feature.Eval(board, true);

            Assert.Equal(0.0, result.Player.HitProbability1);
            Assert.Equal(0.0, result.Player.HitProbability2);
            Assert.Equal(0.75, result.Player.EscapeProbability1);
            Assert.Equal(0.1111111111111111, result.Player.EscapeProbability2);
            Assert.Equal(0.0, result.Opponent.HitProbability1);
            Assert.Equal(0.0, result.Opponent.HitProbability2);
            Assert.Equal(0.75, result.Opponent.EscapeProbability1);
            Assert.Equal(0.1111111111111111, result.Opponent.EscapeProbability2);
        }

        [Fact]
        public void EvalForBlackAndWhiteMirrors()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var board = boardService.CreateBoard();

            var feature = new ContactProbabilityFeature(boardService);
            var resultWhite = feature.Eval(board, true);
            var resultBlack = feature.Eval(board, true);

            Assert.Equal(resultBlack.Opponent.HitProbability1, resultWhite.Player.HitProbability1);
            Assert.Equal(resultBlack.Opponent.HitProbability2, resultWhite.Player.HitProbability2);
            Assert.Equal(resultBlack.Opponent.EscapeProbability1, resultWhite.Player.EscapeProbability1);
            Assert.Equal(resultBlack.Opponent.EscapeProbability2, resultWhite.Player.EscapeProbability2);

            Assert.Equal(resultBlack.Player.HitProbability1, resultWhite.Opponent.HitProbability1);
            Assert.Equal(resultBlack.Player.HitProbability2, resultWhite.Opponent.HitProbability2);
            Assert.Equal(resultBlack.Player.EscapeProbability1, resultWhite.Opponent.EscapeProbability1);
            Assert.Equal(resultBlack.Player.EscapeProbability2, resultWhite.Opponent.EscapeProbability2);

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

            Assert.Equal(0.083333333333333329, result.Player.HitProbability1);
            Assert.Equal(0.0, result.Player.HitProbability2);
            Assert.Equal(1.0, result.Player.EscapeProbability1);
            Assert.Equal(0.3611111111111111, result.Player.EscapeProbability2);
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

            Assert.Equal(0.16666666666666666, result.Player.HitProbability1);
            Assert.Equal(0.027777777777777776, result.Player.HitProbability2);
            Assert.Equal(1.0, result.Player.EscapeProbability1);
            Assert.Equal(0.3611111111111111, result.Player.EscapeProbability2);
        }
    }
}
