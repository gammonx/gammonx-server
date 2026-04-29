using GammonX.Engine.Extensions;
using GammonX.Engine.Services;

using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;
using GammonX.Mars.Server.Services.NN;

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
            var evalService = FeatureEvalServiceFactory.Create(_modus);
            if (_neuralEvalService != null)
            {
                evalService.NeuralEvalService = _neuralEvalService;
            }
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

                var evalRequest = new EvalMoveRequestContract()
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
                    150);

                if (result.BestMove.Moves.Count != 0)
                {
                    _recorder.RecordPosition(result.EvalResult, isWhite);

                    foreach (var move in result.BestMove.Moves)
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                isWhite = !isWhite;
            }

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult([], turnCount, null);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var samples = _recorder.Finalize(whiteWon);

            float? predictionVariance = null;
            if (_neuralEvalService != null)
            {
                var preds = _recorder.NetPredictions;
                if (preds.Count > 1)
                {
                    var mean = preds.Average();
                    predictionVariance = preds.Average(p => (p - mean) * (p - mean));
                }
            }

            return new SelfPlayRunResult(samples, turnCount, predictionVariance);
        }
    }
}
