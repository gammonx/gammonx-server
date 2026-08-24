using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using GammonX.Engine.Models;

using static TorchSharp.torch;

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
        public async Task LinearModelBotCanPlayAgainstItself(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var evalService = FeatureEvalServiceFactory.Create(modus, null!);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = true;
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

                var result = await evalService.EvalMoveSequencesAsync(
                    evalRequest,
                    EvalWeights.GetContactWeights(modus),
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
        public async Task NeuralNetBotCanPlayAgainstItself(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
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

                var result = await evalService.EvalMoveSequencesAsync(
                    evalRequest,
                    EvalWeights.GetContactWeights(modus),
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
        public async Task NeuralNetBotHardDefeatsMedium(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
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

                var result = await evalService.EvalMoveSequencesAsync(
                    evalRequest,
                    EvalWeights.GetContactWeights(modus),
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
            Assert.Equal(0, board.PipCountWhite);
        }

        [Theory]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        public async Task NeuralNetBotMediumDefeatsEasy(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
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

                var result = await evalService.EvalMoveSequencesAsync(
                    evalRequest,
                    EvalWeights.GetContactWeights(modus),
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
            Assert.Equal(0, board.PipCountWhite);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task NeuralNetBotHardOffersDoubleAgainstEasy(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            var whiteBotLevel = BotLevel.Hard;
            var blackBotLevel = BotLevel.Easy;
            const int maxTurns = 1000;
            var turnCount = 0;
            var doubleCount = 0;
            var cubeValue = 1;

            while (board.BearOffCountBlack != board.WinConditionCount
                && board.BearOffCountWhite != board.WinConditionCount
                && turnCount < maxTurns)
            {
                var evalCubePlayerReq = new EvalCubeRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    BotLevel = isWhite ? whiteBotLevel : blackBotLevel,
                    MatchLength = 5,
                    // we force the hard bot to offer a double
                    PointsAwayPlayer = isWhite ? 5 : 1,
                    PointsAwayOpp = isWhite ? 1 : 5
                };

                var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(evalCubePlayerReq);
                Assert.NotEqual(CubeAction.Unknown, shouldOffer);
                Assert.NotEqual(CubeAction.Unknown, shouldTake);

                if (shouldOffer == CubeAction.Double && board is IDoublingCubeModel cubeModel && cubeModel.CanOfferDoublingCube(isWhite))
                {
                    doubleCount++;

                    var evalCubeOppReq = new EvalCubeRequestContract
                    {
                        Board = board.ToContract(false),
                        IsWhite = !isWhite,
                        Modus = modus,
                        BotLevel = !isWhite ? whiteBotLevel : blackBotLevel,
                        MatchLength = 5,
                        // we force the hard bot to offer a double
                        PointsAwayPlayer = isWhite ? 5 : 1,
                        PointsAwayOpp = isWhite ? 1 : 5
                    };

                    var (shouldOfferOpp, shouldTakeOpp) = await evalService.EvalCubeAsync(evalCubeOppReq);
                    Assert.NotEqual(CubeAction.Unknown, shouldOfferOpp);
                    Assert.NotEqual(CubeAction.Unknown, shouldTakeOpp);

                    if (shouldTakeOpp == CubeAction.Take)
                    {
                        cubeModel.AcceptDoublingCubeOffer(!isWhite);
                        Assert.False(cubeModel.CanOfferDoublingCube(isWhite));
                        Assert.True(cubeModel.CanOfferDoublingCube(!isWhite));

                    }
                    else if (shouldTakeOpp == CubeAction.Pass)
                    {
                        Assert.True(cubeModel.CanOfferDoublingCube(isWhite));
                    }
                    else
                    {
                        Assert.Fail("unexpected cube response from opponent");
                    }
                    cubeValue = cubeModel.DoublingCubeValue;
                }

                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1]
                    ? [rolls[0], rolls[0], rolls[0], rolls[0]]
                    : [rolls[0], rolls[1]];

                var evalMoveReq = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls,
                    BotLevel = isWhite ? whiteBotLevel : blackBotLevel
                };

                var result = await evalService.EvalMoveSequencesAsync(
                    evalMoveReq,
                    EvalWeights.GetContactWeights(modus),
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
            Assert.Equal(0, board.PipCountWhite);
            Assert.True(doubleCount > 0);
            Assert.True(cubeValue > 1);
            Assert.Equal(1 * (doubleCount + 1), cubeValue);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task EvaluateMoveSelectionOfTwoPlyAndHard(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = true;
            var whiteBotLevel = BotLevel.TwoPly;
            var blackBotLevel = BotLevel.Hard;
            const int maxTurns = 1000;
            var turnCount = 0;
            var differentSelectionCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount && board.BearOffCountWhite != board.WinConditionCount && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1] ? [rolls[0], rolls[0], rolls[0], rolls[0]] : [rolls[0], rolls[1]];

                var twoPlyRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls,
                    BotLevel = whiteBotLevel
                };

                var onePlyRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls,
                    BotLevel = blackBotLevel
                };

                var twoPlyResult = await evalService.EvalMoveSequencesAsync(twoPlyRequest, EvalWeights.GetContactWeights(modus));
                var onePlyResult = await evalService.EvalMoveSequencesAsync(onePlyRequest, EvalWeights.GetContactWeights(modus));
                if (!twoPlyResult.Equals(onePlyResult))
                {
                    differentSelectionCount++;
                }

                var result = isWhite ? twoPlyResult : onePlyResult;
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
            {
                Assert.Fail("max turn count exceeded, possible infinite loop");
            }

            // we expect white bot (TwoPly) to win
            Assert.True(board.BearOffCountWhite == board.WinConditionCount);
            Assert.Equal(0, board.PipCountWhite);
            Assert.True(differentSelectionCount > 0);
        }
    }
}
