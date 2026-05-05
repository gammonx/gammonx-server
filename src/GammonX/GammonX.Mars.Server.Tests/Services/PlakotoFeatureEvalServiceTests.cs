using GammonX.Engine.Extensions;
using GammonX.Engine.Services;

using GammonX.Mars.NN;
using GammonX.Mars.NN.Services;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace GammonX.Mars.Server.Tests.Services
{
    public class PlakotoFeatureEvalServiceTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(1, 6)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, 5)]
        [InlineData(3, 6)]
        [InlineData(4, 4)]
        [InlineData(4, 5)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 6)]
        public void PlakotoStartBoardEvalEqualsForBlackAndWhite(int roll1, int roll2)
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);

            EvalMoveRequestContract request = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                Rolls = [roll1, roll2],
                IsWhite = true
            };

            EvalWeights.PlakotoContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.PlakotoCheapContactWeights.Validate();

            var evalService = new PlakotoFeatureEvalService(null);
            var result = evalService.EvalMoveSequence(request, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);

            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(1, 6)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, 5)]
        [InlineData(3, 6)]
        [InlineData(4, 4)]
        [InlineData(4, 5)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 6)]
        public void CanEvalPlakotoBoard1ForWhite(int roll1, int roll2)
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            EvalWeights.PlakotoContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.PlakotoCheapContactWeights.Validate();

            var evalService = new PlakotoFeatureEvalService(null);

            EvalMoveRequestContract requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                Rolls = [roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);
            Assert.NotNull(resultWhite);

            EvalMoveRequestContract requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                Rolls = [roll1, roll2],
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(GameModus.Plakoto));
            Assert.Equal(resultWhite.Moves, invertedBlack);
        }

        [Theory]
        [InlineData(3, 5)]
        public void CanEvalPlakotoBoard3ForWhite(int roll1, int roll2)
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard3);
            Assert.NotNull(boardContract);

            EvalWeights.PlakotoContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.PlakotoCheapContactWeights.Validate();

            var evalService = new PlakotoFeatureEvalService(null);

            EvalMoveRequestContract requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                Rolls = [roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);
            Assert.NotNull(resultWhite);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 5)]
        [InlineData(6, 6)]
        public void CanEvalPlakotoBoard4ForWhite(int roll1, int roll2)
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard4);
            Assert.NotNull(boardContract);

            EvalWeights.PlakotoContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.PlakotoCheapContactWeights.Validate();

            var evalService = new PlakotoFeatureEvalService(null);

            EvalMoveRequestContract requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                Rolls = [roll1, roll2, roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);
            Assert.NotNull(resultWhite);
        }

        [Fact]
        public void CanEvalPlakotoBlackWonBoard()
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBlackWonBoard);
            Assert.NotNull(boardContract);

            EvalWeights.PlakotoContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.PlakotoCheapContactWeights.Validate();

            var evalService = new PlakotoFeatureEvalService(null);

            EvalBoardRequestContract requestBlack = new EvalBoardRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                IsWhite = false
            };
            var resultBlack = evalService.EvalBoardState(requestBlack, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights);

            EvalBoardRequestContract requestWhite = new EvalBoardRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Plakoto,
                IsWhite = true
            };
            var resultWhite = evalService.EvalBoardState(requestWhite, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights);

            Assert.True(resultBlack > 0.5);
            Assert.True(resultWhite < -0.5);
        }
    }
}
