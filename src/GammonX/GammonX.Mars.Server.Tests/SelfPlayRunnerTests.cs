using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.NN.Services;
using GammonX.Mars.NN;
using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Tests
{
    public class SelfPlayRunnerTests
    {
        [Theory]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void LinearModelBotCanPlayAgainstItself(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var evalService = FeatureEvalServiceFactory.Create(modus, null!);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount
                && board.BearOffCountWhite != board.WinConditionCount
                && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1]
                    ? [rolls[0], rolls[0], rolls[0], rolls[0]]
                    : [rolls[0], rolls[1]];

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    CheapContactWeightsForModus(modus),
                    ContactWeightsForModus(modus),
                    EvalWeights.RaceWeights,
                    20);

                if (result.Moves.Count != 0)
                {
                    foreach (var move in result.Moves)
                    {
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                    }
                }

                isWhite = !isWhite;
            }

            if (turnCount >= maxTurns)
                Assert.Fail("max turn count exceeded, possible infinite loop");

            Assert.True(board.BearOffCountBlack == board.WinConditionCount || board.BearOffCountWhite == board.WinConditionCount);
            Assert.True(board.PipCountBlack == 0 || board.PipCountWhite == 0);
        }

        [Theory]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void NeuralNetBotCanPlayAgainstItself(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount
                && board.BearOffCountWhite != board.WinConditionCount
                && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1]
                    ? [rolls[0], rolls[0], rolls[0], rolls[0]]
                    : [rolls[0], rolls[1]];

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    CheapContactWeightsForModus(modus),
                    ContactWeightsForModus(modus),
                    EvalWeights.RaceWeights,
                    20);

                if (result.Moves.Count != 0)
                {
                    foreach (var move in result.Moves)
                    {
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                    }
                }

                isWhite = !isWhite;
            }

            if (turnCount >= maxTurns)
                Assert.Fail("max turn count exceeded, possible infinite loop");

            Assert.True(board.BearOffCountBlack == board.WinConditionCount || board.BearOffCountWhite == board.WinConditionCount);
            Assert.True(board.PipCountBlack == 0 || board.PipCountWhite == 0);
        }

        private static ContactWeightModel ContactWeightsForModus(GameModus modus)
        {
            return modus switch
            {
                GameModus.Plakoto => EvalWeights.PlakotoContactWeights,
                GameModus.Fevga => EvalWeights.FevgaContactWeights,
                _ => throw new InvalidOperationException($"Modus {modus} has no contact weights.")
            };
        }

        private static ContactWeightModel CheapContactWeightsForModus(GameModus modus)
        {
            return modus switch
            {
                GameModus.Plakoto => EvalWeights.PlakotoCheapContactWeights,
                GameModus.Fevga => EvalWeights.FevgaCheapContactWeights,
                _ => throw new InvalidOperationException($"Modus {modus} has no contact weights.")
            };
        }
    }
}
