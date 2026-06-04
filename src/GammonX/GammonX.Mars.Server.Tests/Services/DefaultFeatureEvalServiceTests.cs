using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.NN;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Tests.Services
{
    public class DefaultFeatureEvalServiceTests
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
        public void CanEvalBackgammonStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Backgammon;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
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
        public void CanEvalTavlaStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Tavla;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
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
        public void CanEvalPortesStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Portes;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
        }
    }
}
