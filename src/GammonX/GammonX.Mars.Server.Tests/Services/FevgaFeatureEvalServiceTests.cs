using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.Server.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Tests.Services
{
    public class FevgaFeatureEvalServiceTests
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
        public void CanEvalPlakotoBoard1ForWhiteAndBlack(int roll1, int roll2)
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            EvalWeights.FevgaContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.FevgaCheapContactWeights.Validate();

            var evalService = new FevgaFeatureEvalService();
            EvalMoveRequestContract requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Fevga,
                Rolls = new int[] { roll1, roll2 },
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, EvalWeights.FevgaCheapContactWeights, EvalWeights.FevgaContactWeights, EvalWeights.RaceWeights);
            Assert.NotNull(resultWhite);

            EvalMoveRequestContract requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Fevga,
                Rolls = new int[] { roll1, roll2 },
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, EvalWeights.FevgaCheapContactWeights, EvalWeights.FevgaContactWeights, EvalWeights.RaceWeights);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(GameModus.Fevga));
            Assert.Equal(resultWhite.Moves, invertedBlack);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 5)]
        [InlineData(6, 6)]
        public void CanEvalPlakotoBoard1ForDoubles(int roll1, int roll2)
        {
            var boardService = BoardServiceFactory.Create(GameModus.Fevga);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            EvalWeights.FevgaContactWeights.Validate();
            EvalWeights.RaceWeights.Validate();
            EvalWeights.FevgaCheapContactWeights.Validate();

            var evalService = new FevgaFeatureEvalService();
            EvalMoveRequestContract requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Fevga,
                Rolls = new int[] { roll1, roll2, roll1, roll2 },
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, EvalWeights.FevgaCheapContactWeights, EvalWeights.FevgaContactWeights, EvalWeights.RaceWeights);
            Assert.NotNull(resultWhite);

            EvalMoveRequestContract requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = GameModus.Fevga,
                Rolls = new int[] { roll1, roll2, roll1, roll2 },
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, EvalWeights.FevgaCheapContactWeights, EvalWeights.FevgaContactWeights, EvalWeights.RaceWeights);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(GameModus.Fevga));
            Assert.Equal(resultWhite.Moves, invertedBlack);
        }
    }
}
