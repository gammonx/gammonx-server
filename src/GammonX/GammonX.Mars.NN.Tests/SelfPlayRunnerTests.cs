using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests
{
    public class SelfPlayRunnerTests
    {
        [Theory]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
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
                    Rolls = rolls,
                    BotLevel = BotLevel.Hard
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    EvalWeights.GetCheapContactWeights(modus),
                    EvalWeights.GetContactWeights(modus),
                    EvalWeights.GetRaceWeights(modus),
                    150);

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
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
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
                    Rolls = rolls,
                    BotLevel = BotLevel.Hard
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    EvalWeights.GetCheapContactWeights(modus),
                    EvalWeights.GetContactWeights(modus),
                    EvalWeights.GetRaceWeights(modus),
                    150);

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
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        public void NeuralNetBotHardDefeatsMedium(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            var whiteBotLevel = BotLevel.Hard;
            var blackBotLevel = BotLevel.Medium;
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
                    Rolls = rolls,
                    BotLevel = isWhite ? whiteBotLevel : blackBotLevel
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    EvalWeights.GetCheapContactWeights(modus),
                    EvalWeights.GetContactWeights(modus),
                    EvalWeights.GetRaceWeights(modus),
                    150);

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

            // we expect white bot (hard) to win 
            Assert.True(board.BearOffCountWhite == board.WinConditionCount);
            Assert.True(board.PipCountWhite == 0);
        }

        [Theory]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        public void NeuralNetBotMediumDefeatsEasy(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            var whiteBotLevel = BotLevel.Medium;
            var blackBotLevel = BotLevel.Easy;
            const int maxTurns = 1000;
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
                    Rolls = rolls,
                    BotLevel = isWhite ? whiteBotLevel : blackBotLevel
                };

                var result = evalService.EvalMoveSequence(
                    evalRequest,
                    EvalWeights.GetCheapContactWeights(modus),
                    EvalWeights.GetContactWeights(modus),
                    EvalWeights.GetRaceWeights(modus),
                    150);

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

            // we expect white bot (medium) to win 
            Assert.True(board.BearOffCountWhite == board.WinConditionCount);
            Assert.True(board.PipCountWhite == 0);
        }
    }
}
