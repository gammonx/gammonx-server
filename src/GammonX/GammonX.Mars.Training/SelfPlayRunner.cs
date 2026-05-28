using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training
{
    public sealed record SelfPlayRunResult(
        IReadOnlyList<(float[] Features, float Label)> Samples,
        int TurnCount,
        float? PredictionVariance);

    public sealed class SelfPlayRunner
    {
        private readonly GameModus _modus;
        private readonly SelfPlayRecorder _recorder;
        private readonly INeuralEvalService? _neuralEvalService;

        public SelfPlayRunner(SelfPlayRecorder recorder, GameModus modus, INeuralEvalService? neuralService)
        {
            _recorder = recorder;
            _modus = modus;
            _neuralEvalService = neuralService;
        }

        public SelfPlayRunResult Run(
            ContactWeightModel contactWeights,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights)
        {
            var boardService = BoardServiceFactory.Create(_modus);
            var board = boardService.CreateBoard();
            var evalService = FeatureEvalServiceFactory.Create(_modus, _neuralEvalService!);
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
                    Modus = _modus,
                    Rolls = rolls
                };

                var result = evalService.EvalMoveSequenceForTraining(
                    evalRequest,
                    cheapContactWeights,
                    contactWeights,
                    raceWeights,
                    50);

                if (result.Length != 0)
                {
                    // we use epsilon-greediness to occasionally pick a random legal move
                    // for exploration and increase the diversity of training samples
                    // we can start with a higher epsilon in the early turns and decrease it as the game progresses
                    var effectiveEpsilon = turnCount <= 20 ? 0.25f : 0.05f;

                    var resultToPlay = effectiveEpsilon > 0f
                        && _neuralEvalService != null
                        && Random.Shared.NextSingle() < effectiveEpsilon
                            ? result[Random.Shared.Next(result.Length)]
                            : result[0]; // best move sequences

                    foreach (var move in resultToPlay.Move.Moves)
                    {
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                    }

                    _recorder.RecordPosition(resultToPlay.EvalResult, board, isWhite);
                }

                isWhite = !isWhite;

                if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
                {
                    // draw: both mothers pinned, neither player can win
                    // label all recorded positions as 0.5 (half-win) rather than discarding
                    var drawSamples = _recorder.Finalize(whiteWon: null);
                    return new SelfPlayRunResult(drawSamples, turnCount, null);
                }
            }

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult([], turnCount, null);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var samples = _recorder.Finalize(whiteWon);
                        
            float? predictionVariance = null;
            if (_neuralEvalService != null)
            {
                var predictions = _recorder.NetPredictions;
                if (predictions.Count > 1)
                {
                    var mean = predictions.Average();
                    predictionVariance = predictions.Average(p => (p - mean) * (p - mean));
                }
            }

            return new SelfPlayRunResult(samples, turnCount, predictionVariance);
        }
    }
}
