using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Mars.Training.Sidecars;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Mars.Training.Generator
{
    /// <summary>
    /// Runs a single self-play game and records training samples for each position encountered.
    /// </summary>
    /// <param name="Samples">List of (features, label) pairs for each position encountered during the game.</param>
    /// <param name="TurnCount">The number of turns played in the game.</param>
    /// <param name="PredictionVariance">The variance of the network's predictions during the game, if available.</param>
    /// <param name="Trajectory">The recorded game trajectory, if trajectory recording was enabled.</param>
    /// <param name="ExplorationDecisions">The exploration decisions made during the game, if diagnostics were enabled.</param>
    /// <param name="ConstraintMetrics">The aggregate prediction-constraint metrics, if collection was enabled.</param>
    /// <param name="SearchDecisions">The selective two-ply search decisions made during the game, if selective two-ply search was enabled.</param>
    public sealed record SelfPlayRunResult(
        IReadOnlyList<(float[] Features, float[] Label)> Samples,
        int TurnCount,
        float? PredictionVariance,
        GameTrajectory? Trajectory = null,
        IReadOnlyList<ExplorationDecision>? ExplorationDecisions = null,
        ConstraintMetricsResult? ConstraintMetrics = null,
        IReadOnlyList<SelectiveTwoPlySearchDecision>? SearchDecisions = null);

    public sealed record SelfPlayEntry(INeuralEvalService? EvalService, BotLevel BotLevel);

    public sealed class SelfPlayRunner
    {
        private readonly GameModus _modus;
        private readonly SelfPlayRecorder _recorder;
        private readonly SelfPlayEntry? _entryA;
        private readonly SelfPlayEntry? _entryB;
        private readonly ExplorationOptions _explorationOptions;
        private readonly SelectiveTwoPlyOptions _selectiveTwoPlyOptions;
        private readonly List<ExplorationDecision> _explorationDecisions = new List<ExplorationDecision>();
        private readonly List<SelectiveTwoPlySearchDecision> _searchDecisions = new List<SelectiveTwoPlySearchDecision>();

        public SelfPlayRunner(
            SelfPlayRecorder recorder,
            GameModus modus,
            SelfPlayEntry? entryA,
            SelfPlayEntry? entryB = null,
            ExplorationOptions? explorationOptions = null,
            SelectiveTwoPlyOptions? selectiveTwoPlyOptions = null)
        {
            if (entryB != null && entryA == null)
                throw new ArgumentException("Model B requires Model A.", nameof(entryB));
            if (entryB?.EvalService == null && entryB != null)
                throw new ArgumentException("Model B requires an evaluation service.", nameof(entryB));

            _recorder = recorder;
            _modus = modus;
            _entryA = entryA;
            _entryB = entryB;
            _explorationOptions = explorationOptions ?? new ExplorationOptions();
            _explorationOptions.Validate();
            _selectiveTwoPlyOptions = selectiveTwoPlyOptions ?? new SelectiveTwoPlyOptions();
            _selectiveTwoPlyOptions.Validate();

            if (_selectiveTwoPlyOptions.Enabled && _entryA?.EvalService == null)
                throw new ArgumentException("Selective two-ply search requires a neural Model A.", nameof(selectiveTwoPlyOptions));
            if (_selectiveTwoPlyOptions.Enabled && _entryA?.BotLevel == BotLevel.TwoPly)
                throw new ArgumentException("Selective two-ply search requires a one-ply Model A bot level.", nameof(selectiveTwoPlyOptions));
        }

        public async Task<SelfPlayRunResult> RunAsync(ContactWeightModel contactWeights, bool? modelAIsWhite = null)
        {
            var boardService = BoardServiceFactory.Create(_modus);
            var board = boardService.CreateBoard();
            var modelAEvalService = FeatureEvalServiceFactory.Create(_modus, _entryA?.EvalService!);
            var modelBEvalService = _entryB?.EvalService == null ? null : FeatureEvalServiceFactory.Create(_modus, _entryB.EvalService);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var modelAWhite = modelAIsWhite ?? Random.Shared.Next(2) == 0;
            var isWhite = _entryB == null ? modelAWhite : modelAIsWhite ?? Random.Shared.Next(2) == 0;

            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount && board.BearOffCountWhite != board.WinConditionCount && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1] ? [rolls[0], rolls[0], rolls[0], rolls[0]] : [rolls[0], rolls[1]];

                // We append the roll event because the turn number depends on it
                boardService.AddRollEventToHistory(board, isWhite, rolls);

                var isModelATurn = _entryB == null || isWhite == modelAWhite;
                var activeEvalService = isModelATurn ? modelAEvalService : modelBEvalService ?? modelAEvalService;

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = _modus,
                    Rolls = rolls,
                    BotLevel = isModelATurn ? _entryA?.BotLevel ?? BotLevel.Hard : _entryB?.BotLevel ?? BotLevel.Hard
                };
                
                var moveSequences = await activeEvalService.EvalMoveSequencesForTrainingAsync(evalRequest, contactWeights);

                if (moveSequences.Count != 0)
                {
                    var resultToPlay = isModelATurn
                        ? await SelectTrainingMoveAsync(
                            modelAEvalService,
                            boardService,
                            board,
                            evalRequest.Board,
                            isWhite,
                            rolls,
                            moveSequences,
                            contactWeights,
                            turnCount,
                            false)
                        : moveSequences[0];

                    foreach (var move in resultToPlay.MoveSequence.Moves)
                    {
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                    }

                    await _recorder.RecordPositionAsync(resultToPlay.EvalResult, board, isWhite);
                }
                else
                {
                    var passEval = activeEvalService.EvalPositionForTraining(board.ToContract(false), isWhite);
                    await _recorder.RecordPositionAsync(passEval, board, isWhite);
                }

                isWhite = !isWhite;

                if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
                {
                    // draw: both mothers pinned, neither player can win
                    // label all recorded positions as 0.5 (half-win) rather than discarding
                    var finalizedDraw = _recorder.Finalize(GameResult.Draw, GameResult.Draw, true);
                    return new SelfPlayRunResult(
                        finalizedDraw.Samples,
                        turnCount,
                        null,
                        finalizedDraw.Trajectory,
                        _explorationDecisions,
                        _recorder.ConstraintMetrics,
                        _searchDecisions);
                }
            }

            if (turnCount >= maxTurns)
                return new SelfPlayRunResult(
                    new List<(float[], float[])>(),
                    turnCount,
                    null,
                    null,
                    _explorationDecisions,
                    _recorder.ConstraintMetrics,
                    _searchDecisions);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var gameResult = whiteWon ? board.ToGameResult(Guid.Empty, true) : board.ToGameResult(Guid.Empty, false);
            // we pass both winner and loser results so the recorder can label each position correctly
            var finalized = _recorder.Finalize(gameResult.WinnerResult, gameResult.LoserResult, whiteWon);

            var predictionVariance = ComputePredVariance();

            return new SelfPlayRunResult(
                finalized.Samples,
                turnCount,
                predictionVariance,
                finalized.Trajectory,
                _explorationDecisions,
                _recorder.ConstraintMetrics,
                _searchDecisions);
        }

        public async Task<SelfPlayRunResult> RunAgainstBotServiceGameAsync(GameModus modus, bool evalPlayerIsWhite, ContactWeightModel contactWeights)
        {
            if (_entryB?.EvalService != null)
                throw new InvalidOperationException("Model B cannot be used when playing against the WildBG bot service.");

            var diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(diceFactory);
            var matchFactory = new MatchSessionFactory(gameSessionFactory);
            var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(From(modus), MatchType.CashGame, matchFactory);
            var boardService = BoardServiceFactory.Create(modus);

            // eval service to test
            var evalService = FeatureEvalServiceFactory.Create(modus, _entryA?.EvalService!);
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
            var board = gameSession!.BoardModel;

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
                    nextMoves = await wildBgService.GetNextMovesAsync(matchSession, activePlayerId);
                    var boardContract = board.ToContract(false);
                    evalResultModel = await evalService.EvalMoveSequenceAsync(boardContract, isWhite, nextMoves, contactWeights);
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
                        BotLevel = _entryA?.BotLevel ?? BotLevel.Hard
                    };
                    var result = await evalService.EvalMoveSequencesForTrainingAsync(evalRequest, contactWeights);

                    if (result.Count != 0)
                    {
                        evalResultModel = await SelectTrainingMoveAsync(
                            evalService,
                            boardService,
                            board,
                            evalRequest.Board,
                            isWhite,
                            rolls,
                            result,
                            contactWeights,
                            turnCount,
                            true);

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
                await _recorder.RecordPositionAsync(evalResultModel.EvalResult, board, isWhite);

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
            {
                return new SelfPlayRunResult([], turnCount, null, null, _explorationDecisions, _recorder.ConstraintMetrics, _searchDecisions);
            }

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var gameResult = whiteWon ? board.ToGameResult(Guid.Empty, true) : board.ToGameResult(Guid.Empty, false);
            // we pass both winner and loser results so the recorder can label each position correctly
            var finalized = _recorder.Finalize(gameResult.WinnerResult, gameResult.LoserResult, whiteWon);

            var predictionVariance = ComputePredVariance();

            return new SelfPlayRunResult(
                finalized.Samples,
                turnCount,
                predictionVariance,
                finalized.Trajectory,
                _explorationDecisions,
                _recorder.ConstraintMetrics,
                _searchDecisions);
        }

        /// <summary>
        /// Calculates the population variance of the networks win predictions recorded during the game.
        /// </summary>
        /// <returns>
        /// The variance of the first prediction head when a neural evaluator produced more than one
        /// prediction; otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// The calculation uses population variance because the collected predictions represent the
        /// complete set of positions from this self-play game rather than a sample of a larger set.
        /// </remarks>
        private float? ComputePredVariance()
        {
            float? predictionVariance = null;
            if (_entryA != null)
            {
                var predictions = _recorder.NetPredictions;
                if (predictions.Count > 1)
                {
                    // The first output is the predicted probability of a win.
                    var pWins = predictions.Select(p => p[0]).ToList();
                    var mean = pWins.Average();
                    // Average squared deviations from the game-level mean (population variance).
                    predictionVariance = pWins.Average(p => (p - mean) * (p - mean));
                }
            }

            return predictionVariance;
        }

        /// <summary>
        /// Selects the move to play according to the configured exploration policy and records diagnostics.
        /// </summary>
        /// <param name="evalService">The evaluator used to score a move selected outside the ranked results.</param>
        /// <param name="boardService">The board service used to enumerate legal moves.</param>
        /// <param name="board">The current mutable board.</param>
        /// <param name="boardContract">The current board in evaluator contract form.</param>
        /// <param name="isWhite">Whether the side selecting the move is white.</param>
        /// <param name="rolls">The dice rolls available for the current turn.</param>
        /// <param name="rankedResults">Candidate moves ordered from highest to lowest score.</param>
        /// <param name="contactWeights">The weights used when re-evaluating a random legal move.</param>
        /// <param name="turnCount">The one-based turn number used by the exploration policy and diagnostics.</param>
        /// <param name="againstBot">Whether this decision is being made in a game against another bot service.</param>
        /// <returns>The evaluated move sequence selected by the exploration policy.</returns>
        /// <exception cref="InvalidOperationException">Thrown when ranked results are not ordered by descending score.</exception>
        /// <remarks>
        /// Ranked exploration samples from the configured top-ranked candidates, greedy exploration
        /// selects the best candidate, and random exploration samples from all legal moves. A random
        /// move is evaluated again so its recorded score corresponds to the move actually played.
        /// </remarks>
        private async Task<FinalEvalResultModel> SelectTrainingMoveAsync(
            IFeatureEvalService evalService,
            IBoardService boardService,
            IBoardModel board,
            BoardModelContract boardContract,
            bool isWhite,
            int[] rolls,
            FinalEvalResultModels rankedResults,
            ContactWeightModel contactWeights,
            int turnCount,
            bool againstBot)
        {
            if (_selectiveTwoPlyOptions.Enabled)
            {
                var selectiveResult = await ApplySelectiveTwoPlyAsync(
                    evalService,
                    boardContract,
                    isWhite,
                    rankedResults,
                    contactWeights,
                    _selectiveTwoPlyOptions,
                    Random.Shared.NextSingle(),
                    _recorder.GameId,
                    _modus,
                    turnCount,
                    againstBot);
                rankedResults = selectiveResult.Results;

                if (_selectiveTwoPlyOptions.CollectDiagnostics)
                    _searchDecisions.Add(selectiveResult.Decision);
            }

            var bestScore = rankedResults[0].Score;
            double? secondBestScore = rankedResults.Count > 1 ? rankedResults[1].Score : null;
            // A missing second candidate means that no meaningful score gap can be computed.
            double? scoreGap = secondBestScore.HasValue ? bestScore - secondBestScore.Value : null;

            if (scoreGap.HasValue && scoreGap.Value < 0d)
            {
                throw new InvalidOperationException("Move evaluation results must be sorted from highest to lowest score.");
            }

            // The gap is supplied to the policy because confidence in the best move can influence exploration.
            var choice = RankAwareExplorationPolicy.Select(
                turnCount,
                rankedResults.Count,
                _entryA != null,
                Random.Shared.NextSingle(),
                _explorationOptions,
                scoreGap);

            FinalEvalResultModel selectedResult;
            int? selectedRank;
            if (choice == ExplorationChoice.Ranked)
            {
                // Sample within the configured top-ranked window, then map that sample to a candidate index.
                var topRankedCount = Math.Min(_explorationOptions.TopRankedCount, rankedResults.Count);
                var randomIndex = Random.Shared.Next(topRankedCount);
                var rankedIndex = RankAwareExplorationPolicy.GetRankedCandidateIndex(
                    rankedResults.Count,
                    randomIndex,
                    _explorationOptions);
                selectedResult = rankedResults[rankedIndex];
                selectedRank = rankedIndex;
            }
            else if (choice == ExplorationChoice.Greedy)
            {
                // Greedy selection always uses the highest-scoring candidate.
                selectedResult = rankedResults[0];
                selectedRank = 0;
            }
            else
            {
                // Random exploration considers every currently legal move, not only evaluated candidates.
                var legalMoves = GetAllLegalExplorationMoves(boardService, board, isWhite, rolls);
                if (legalMoves.Length == 0)
                {
                    // Fall back to the best ranked result when the position has no legal move sequence.
                    selectedResult = rankedResults[0];
                    selectedRank = 0;
                }
                else
                {
                    var selectedMove = legalMoves[Random.Shared.Next(legalMoves.Length)];
                    // Re-evaluate rare random moves so the recorded value belongs to the move we actually play.
                    selectedResult = await evalService.EvalMoveSequenceAsync(boardContract, isWhite, selectedMove, contactWeights);
                    selectedRank = null;
                }
            }

            if (_explorationOptions.CollectScoreGapDiagnostics)
            {
                // Record the decision after selection so the selected score and rank describe the move played.
                _explorationDecisions.Add(new ExplorationDecision(
                    _recorder.GameId,
                    _modus,
                    turnCount,
                    turnCount <= _explorationOptions.EarlyTurnCount,
                    againstBot,
                    rankedResults.Count,
                    bestScore,
                    secondBestScore,
                    scoreGap,
                    choice,
                    selectedRank,
                    selectedResult.Score));
            }

            return selectedResult;
        }

        internal static async Task<(FinalEvalResultModels Results, SelectiveTwoPlySearchDecision Decision)> ApplySelectiveTwoPlyAsync(
            IFeatureEvalService evalService,
            BoardModelContract boardContract,
            bool isWhite,
            FinalEvalResultModels rankedResults,
            ContactWeightModel contactWeights,
            SelectiveTwoPlyOptions options,
            float auditRoll,
            Guid gameId,
            GameModus modus,
            int turnIndex,
            bool againstBot)
        {
            ArgumentNullException.ThrowIfNull(evalService);
            ArgumentNullException.ThrowIfNull(boardContract);
            ArgumentNullException.ThrowIfNull(rankedResults);
            ArgumentNullException.ThrowIfNull(options);

            if (rankedResults.Count == 0)
                throw new ArgumentException("At least one ranked result is required.", nameof(rankedResults));

            var onePlyBestScore = rankedResults[0].Score;
            double? onePlySecondBestScore = rankedResults.Count > 1 ? rankedResults[1].Score : null;
            double? onePlyScoreGap = onePlySecondBestScore.HasValue
                ? onePlyBestScore - onePlySecondBestScore.Value
                : null;

            var policyDecision = SelectiveTwoPlyPolicy.Select(
                rankedResults.Count,
                onePlyScoreGap,
                auditRoll,
                options);

            var finalResults = rankedResults;
            double? twoPlyBestScore = null;
            double? twoPlySecondBestScore = null;
            double? twoPlyScoreGap = null;
            bool? bestMoveChanged = null;
            int? twoPlyBestOnePlyRank = null;

            if (policyDecision.ShouldEvaluate)
            {
                // We fetch the candidates based on the count
                var candidates = rankedResults
                    .Take(policyDecision.CandidateCount)
                    .Select(result => result.MoveSequence)
                    .ToArray();
                // We execute a full 2-ply search for the given top candidates
                var evaluatedResults = await evalService.EvalMoveSequenceCandidatesAsync(
                    boardContract,
                    isWhite,
                    candidates,
                    contactWeights,
                    BotLevel.TwoPly);

                if (evaluatedResults.Count != candidates.Length)
                    throw new InvalidOperationException("Candidate evaluation must return one result per requested move sequence.");

                var originalBestIndex = rankedResults.FindIndex(result =>
                    result.MoveSequence.Equals(evaluatedResults[0].MoveSequence));
                if (originalBestIndex < 0)
                    throw new InvalidOperationException("Candidate evaluation returned an unknown move sequence.");

                twoPlyBestOnePlyRank = originalBestIndex + 1;
                twoPlyBestScore = evaluatedResults[0].Score;
                twoPlySecondBestScore = evaluatedResults.Count > 1 ? evaluatedResults[1].Score : null;
                twoPlyScoreGap = twoPlySecondBestScore.HasValue ? twoPlyBestScore.Value - twoPlySecondBestScore.Value : null;
                bestMoveChanged = originalBestIndex != 0;

                finalResults = policyDecision.CandidateCount == rankedResults.Count
                    ? evaluatedResults
                    : new FinalEvalResultModels(
                        evaluatedResults.Concat(rankedResults.Skip(policyDecision.CandidateCount)));
            }

            var searchDecision = new SelectiveTwoPlySearchDecision(
                gameId,
                modus,
                turnIndex,
                againstBot,
                rankedResults.Count,
                policyDecision.CandidateCount,
                options.MaximumCandidates,
                onePlyBestScore,
                onePlySecondBestScore,
                onePlyScoreGap,
                policyDecision.Reason,
                twoPlyBestScore,
                twoPlySecondBestScore,
                twoPlyScoreGap,
                bestMoveChanged,
                twoPlyBestOnePlyRank);

            return (finalResults, searchDecision);
        }

        internal static MoveSequenceModel[] GetAllLegalExplorationMoves(
            IBoardService boardService,
            IBoardModel board,
            bool isWhite,
            int[] rolls)
        {
            return boardService.GetUniqueLegalMoveSequences(board, isWhite, rolls);
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
