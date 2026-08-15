using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Xunit;

using MatchType = GammonX.Models.Enums.MatchType;

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
        float? PredictionVariance,
        GameTrajectory? Trajectory = null);

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

                // We append the roll event because the turn number depends on it
                boardService.AddRollEventToHistory(board, isWhite, rolls);

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = _modus,
                    Rolls = rolls,
                    BotLevel = BotLevel.Hard
                };

                var result = evalService.EvalMoveSequencesForTraining(
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
                else
                {
                    var passEval = evalService.EvalPositionForTraining(board.ToContract(false), isWhite);
                    _recorder.RecordPosition(passEval, board, isWhite);
                }

                isWhite = !isWhite;

                if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
                {
                    // draw: both mothers pinned, neither player can win
                    // label all recorded positions as 0.5 (half-win) rather than discarding
                    var finalizedDraw = _recorder.Finalize(GameResult.Draw, GameResult.Draw, true);
                    return new SelfPlayRunResult(finalizedDraw.Samples, turnCount, null, finalizedDraw.Trajectory);
                }
            }

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult([], turnCount, null);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var gameResult = whiteWon ? board.ToGameResult(Guid.Empty, true) : board.ToGameResult(Guid.Empty, false);
            // we pass both winner and loser results so the recorder can label each position correctly
            var finalized = _recorder.Finalize(gameResult.WinnerResult, gameResult.LoserResult, whiteWon);

            var predictionVariance = ComputePredVariance();

            return new SelfPlayRunResult(finalized.Samples, turnCount, predictionVariance, finalized.Trajectory);
        }

        public SelfPlayRunResult RunAgainstBotServiceGame(
            GameModus modus,
            bool evalPlayerIsWhite,
            ContactWeightModel contactWeights,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights)
        {
            var diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(diceFactory);
            var matchFactory = new MatchSessionFactory(gameSessionFactory);
            var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(From(modus), MatchType.CashGame, matchFactory);

            // eval service to test
            var evalService = FeatureEvalServiceFactory.Create(modus, _neuralEvalService!);
            // bot service to play against
            var wildBgService = BotUtils.GetBotService(WellKnownBotServices.WildBg);

            matchSession.Player1.AcceptNextGame();
            matchSession.Player2.AcceptNextGame();

            var evalPlayerId = evalPlayerIsWhite ? matchSession.Player1.Id : matchSession.Player2.Id;
            var wildbgPlayerId = evalPlayerIsWhite ? matchSession.Player2.Id : matchSession.Player1.Id;

            var activePlayerId = Guid.Empty;
            var otherPlayerId = Guid.Empty;

            if (evalPlayerIsWhite)
            {
                matchSession.StartMatch(evalPlayerId);
                activePlayerId = evalPlayerId;
                otherPlayerId = wildbgPlayerId;
            }
            else
            {
                matchSession.StartMatch(wildbgPlayerId);
                activePlayerId = wildbgPlayerId;
                otherPlayerId = evalPlayerId;
            }

            const int maxTurns = 250;
            var turnCount = 0;

            var gameSession = matchSession.GetGameSession(1);
            Assert.NotNull(gameSession);
            var board = gameSession.BoardModel;

            do
            {
                turnCount++;
                var isWhite = activePlayerId == evalPlayerId
                    ? evalPlayerIsWhite
                    : !evalPlayerIsWhite;

                if (gameSession.Phase == GamePhase.WaitingForRoll)
                {
                    matchSession.RollDices(activePlayerId);
                }

                MoveSequenceModel nextMoves;
                FinalEvalResultModel? evalResultModel = null;
                if (activePlayerId == wildbgPlayerId)
                {
                    // wildbg turn
                    nextMoves = wildBgService.GetNextMovesAsync(matchSession, activePlayerId).ConfigureAwait(false).GetAwaiter().GetResult();
                    var boardContract = board.ToContract(false);
                    evalResultModel = evalService.EvalMoveSequence(boardContract, isWhite, nextMoves, cheapContactWeights, contactWeights, raceWeights);
                }
                else
                {
                    // eval service turn
                    var rolls = gameSession.DiceRolls.Select(dr => dr.Roll).ToArray();
                    var evalRequest = new EvalMoveRequestContract
                    {
                        Board = board.ToContract(false),
                        IsWhite = isWhite,
                        Modus = modus,
                        Rolls = rolls,
                        BotLevel = BotLevel.Hard
                    };
                    var result = evalService.EvalMoveSequencesForTraining(
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

                        evalResultModel = effectiveEpsilon > 0f
                                          && _neuralEvalService != null
                                          && Random.Shared.NextSingle() < effectiveEpsilon
                            ? result[Random.Shared.Next(result.Count)]
                            : result[0]; // best move sequences

                        nextMoves = evalResultModel.MoveSequence;
                    }
                    else
                    {
                        var passEval = evalService.EvalPositionForTraining(board.ToContract(false), isWhite);
                        evalResultModel = new FinalEvalResultModel(0, new MoveSequenceModel(), passEval);
                        nextMoves = new MoveSequenceModel();
                    }
                }

                var hasWon = false;
                foreach (var nextMove in nextMoves.Moves)
                {
                    hasWon = matchSession.MoveCheckers(activePlayerId, nextMove.From, nextMove.To);
                    if (hasWon)
                        break;
                }

                // we must record after the moves were made
                _recorder.RecordPosition(evalResultModel.EvalResult, board, isWhite);

                if (!hasWon)
                {
                    matchSession.EndTurn(activePlayerId);
                    activePlayerId = otherPlayerId;
                    otherPlayerId = activePlayerId == evalPlayerId ? wildbgPlayerId : evalPlayerId;
                }
                else
                {
                    // we only support the first game of a match (cash game)
                    break;
                }
            }
            while (turnCount < maxTurns);

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult([], turnCount, null);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var gameResult = whiteWon ? board.ToGameResult(Guid.Empty, true) : board.ToGameResult(Guid.Empty, false);
            // we pass both winner and loser results so the recorder can label each position correctly
            var finalized = _recorder.Finalize(gameResult.WinnerResult, gameResult.LoserResult, whiteWon);

            var predictionVariance = ComputePredVariance();

            return new SelfPlayRunResult(finalized.Samples, turnCount, predictionVariance, finalized.Trajectory);
        }

        private float? ComputePredVariance()
        {
            float? predictionVariance = null;
            if (_neuralEvalService != null)
            {
                var predictions = _recorder.NetPredictions;
                if (predictions.Count > 1)
                {
                    var pWins = predictions.Select(p => p[0]).ToList();
                    var mean = pWins.Average();
                    predictionVariance = pWins.Average(p => (p - mean) * (p - mean));
                }
            }

            return predictionVariance;
        }

        private static MatchVariant From(GameModus modus)
        {
            switch (modus)
            {
                case GameModus.Backgammon:
                    return MatchVariant.Backgammon;
                case GameModus.Fevga:
                case GameModus.Plakoto:
                case GameModus.Portes:
                    return MatchVariant.Tavli;
                case GameModus.Tavla:
                    return MatchVariant.Tavla;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modus), modus, null);
            }
        }
    }
}
