using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training
{
    /// <summary>
    /// Runs a single self-play game and records training samples for each position encountered.
    /// </summary>
    /// <param name="Samples">List of (features, label) pairs for each position encountered during the game.</param>
    /// <param name="TurnCount">The number of turns played in the game.</param>
    /// <param name="PredictionVariance">The variance of the network's predictions during the game, if available.</param>
    public sealed record SelfPlayRunResult(
        IReadOnlyList<(float[] Features, float[] Label)> Samples,
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
                    Rolls = rolls,
                    BotLevel = BotLevel.Hard
                };

                var result = evalService.EvalMoveSequenceForTraining(
                    evalRequest,
                    cheapContactWeights,
                    contactWeights,
                    raceWeights,
                    150);

                if (result.Count != 0)
                {
                    // we use epsilon-greediness to occasionally pick a random legal move
                    // for exploration and increase the diversity of training samples
                    // we can start with a higher epsilon in the early turns and decrease it as the game progresses
                    var effectiveEpsilon = turnCount <= 20 ? 0.25f : 0.05f;

                    var resultToPlay = effectiveEpsilon > 0f
                        && _neuralEvalService != null
                        && Random.Shared.NextSingle() < effectiveEpsilon
                            ? result[Random.Shared.Next(result.Count)]
                            : result[0]; // best move sequences

                    foreach (var move in resultToPlay.MoveSequence.Moves)
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
                    var drawSamples = _recorder.Finalize(GameResult.Draw, GameResult.Draw, true);
                    return new SelfPlayRunResult(drawSamples, turnCount, null);
                }
            }

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult([], turnCount, null);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var gameResult = whiteWon ? board.ToGameResult(Guid.Empty, true) : board.ToGameResult(Guid.Empty, false);
            // we pass both winner and loser results so the recorder can label each position correctly
            var samples = _recorder.Finalize(gameResult.WinnerResult, gameResult.LoserResult, whiteWon);

            float? predictionVariance = null;
            if (_neuralEvalService != null)
            {
                var predictions = _recorder.NetPredictions;
                if (predictions.Count > 1)
                {
                    var pWins = predictions.Select(p => p[0]);
                    var mean = pWins.Average();
                    predictionVariance = pWins.Average(p => (p - mean) * (p - mean));
                }
            }

            return new SelfPlayRunResult(samples, turnCount, predictionVariance);
        }
    }
}
