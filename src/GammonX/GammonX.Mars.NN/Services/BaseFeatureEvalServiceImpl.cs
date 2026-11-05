using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Engine.Extensions;

using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public abstract class BaseFeatureEvalServiceImpl : IFeatureEvalService
    {
        private readonly INeuralEvalService? _neuralEvalService;

        /// <summary>
        /// Gets or sets the number of concurrent candidates to evaluate in parallel. Default is 1.
        /// </summary>
        /// <remarks>
        /// Although the throughput gains were significant, the fairness gate and the latency gains were not fully verified for parallel evaluation, so we keep the default at 1 for now.
        /// </remarks>
        internal int ParallelCandidateCount { get; set; } = 1;

        /// <summary>
        /// If set to true, each inference batch is filled and may contain replies from multiple rolls; if false, each batch is flushed after each roll.
        /// </summary>
        /// <remarks>
        /// Although the latency gains were not fully verified, we do want to activate the option for now.
        /// </remarks>
        internal bool BatchAcrossRolls { get; set; } = true;

        /// <summary>
        /// If set to true, the working board used for scoring is reused for each inference reply; if false, a new board is cloned for each reply.
        /// Reused in this specific case means that the move sequences are executed, feature vectors are extracted and the moves undone before the inference is started.
        /// </summary>
        /// <remarks>
        /// Although the latency gains were not fully verified, we do want to activate the option for now.
        /// </remarks>
        internal bool ReuseReplyBoard { get; set; } = true;

        /// <summary>
        /// If set to true, the feature vector buffers belong to one root search and are reused only after successful inference completion.
        /// </summary>
        /// <remarks>
        /// Although the latency gains were not fully verified, we do want to activate the option for now.
        /// </remarks>
        internal bool ReuseFeatureBuffers { get; set; } = true;

        protected abstract IBoardService BoardService { get; }


        protected BaseFeatureEvalServiceImpl(INeuralEvalService? neuralEvalService)
        {
            _neuralEvalService = neuralEvalService;
        }

        // <inheritdoc />
        public async Task<(CubeAction ShouldOffer, CubeAction ShouldTake)> EvalCubeAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.EvalCubeRequest);
            ArgumentNullException.ThrowIfNull(options.IsWhite);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);

            if (_neuralEvalService == null)
                throw new InvalidOperationException("Neural evaluation service is required for cube evaluation.");

            var contract = options.EvalCubeRequest;
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = options.IsWhite.Value;
            var turnNumber = options.TurnNumber.Value;

            if (board is IDoublingCubeModel cubeModel)
            {
                var eval = CalculateEvalModelWithDiagnostics(board, turnNumber, isWhite);

                var normalizedEvalResult = NormalizedEvalResultModel.From(eval);
                var predictions = await _neuralEvalService.PredictAsync(normalizedEvalResult, board, isWhite);
                // we calculate the game equity
                var outcome = new GameOutcomeModel(predictions);
                var equityModel = new GameEquityModel(outcome);

                var cubeValue = cubeModel.DoublingCubeValue;

                // match equity without doubling (game continues at current cube value)
                var noDouble = MatchEquityCalculator.CalculateEquity(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeValue);
                // match equity if player doubles and opponent passes (game ends, player wins cube value)
                var equityIfOppPasses = MatchEquityCalculator.GetMET(
                    contract.PointsAwayPlayer - cubeValue,
                    contract.PointsAwayOpp);
                // match equity if opponent doubles and player passes (game ends, opponent wins cube value)
                var equityIfPlayerPasses = MatchEquityCalculator.GetMET(
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp - cubeValue);
                // match equity if opponent redoubles after taking the players double offer
                var equityIfOppTakes = MatchEquityCalculator.CalculateEquityAfterAcceptedDouble(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeValue,
                    currentPlayerIsTaker: false);
                // match equity if player redoubles after taking the opponents double offer
                var equityIfPlayerTakes = MatchEquityCalculator.CalculateEquityAfterAcceptedDouble(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeValue,
                    currentPlayerIsTaker: true);

                CubeAction shouldOffer;
                CubeAction shouldTake;

                // we expect the opponent takes if their equity from taking >= their equity from passing:
                if (equityIfOppTakes <= equityIfOppPasses)
                {
                    // we expect the opponent would take; compare the accepted double with no double.
                    shouldOffer = equityIfOppTakes > noDouble ? CubeAction.Double : CubeAction.NoDouble;
                }
                else
                {
                    // we expect opponent would pass
                    if (noDouble > equityIfOppPasses)
                    {
                        // we expect playing on for gammon/backgammon is better than forcing a pass
                        shouldOffer = CubeAction.TooGood;
                    }
                    else
                    {
                        // we expect forcing a pass is better than playing on
                        shouldOffer = CubeAction.Double;
                    }
                }

                // we expect the player takes if their accepted-double equity is at least their pass equity.
                if (equityIfPlayerTakes >= equityIfPlayerPasses)
                {
                    shouldTake = CubeAction.Take;
                }
                else
                {
                    shouldTake = CubeAction.Pass;
                }

                return new(shouldOffer, shouldTake);
            }

            throw new InvalidDataException($"Game modus '{contract.Modus.GetName()}' does not support doubling cube evaluation.");
        }

        // <inheritdoc />
        public async Task<EvalResult> EvalBoardStateAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.EvalBoardRequest);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);
            ArgumentNullException.ThrowIfNull(options.ContactWeights);

            var contract = options.EvalBoardRequest;
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;
            var searchDepth = contract.BotLevel.ToSearchDepth();
            var turnNumber = options.TurnNumber.Value;

            if (searchDepth == SearchDepth.TwoPly && _neuralEvalService != null)
            {
                // we calculate the eval based on a two-ply evaluation of the resulting board state
                var twoPlyEval = await CalculateTwoPlyEquityAsync(board, turnNumber, isWhite);
                return twoPlyEval;
            }

            var eval = CalculateEvalModelWithDiagnostics(board, turnNumber, isWhite);
            return await EvalPositionAsync(board, isWhite, eval, options.ContactWeights);
        }

        // <inheritdoc />
        public async Task<MoveSequenceModel> EvalMoveSequencesAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.EvalMoveRequest);
            var evalMoves = await EvalMoveSequencesForTrainingAsync(options);
            return evalMoves.Select(options.EvalMoveRequest.BotLevel);
        }

        // <inheritdoc />
        public async Task<FinalEvalResultModels> EvalMoveSequencesForTrainingAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.EvalMoveRequest);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);
            ArgumentNullException.ThrowIfNull(options.ContactWeights);

            var contract = options.EvalMoveRequest;
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = BoardService.CreateBoard(boardContract);
            // we only evaluate move sequences which result in a unique end board state
            var legalMovesSeq = BoardService.GetUniqueLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return [];

            var evalResult = await GetCandidatesByEvalAsync(new FeatureEvalOptions
            {
                Candidates = legalMovesSeq,
                IsWhite = isWhite,
                BotLevel = contract.BotLevel,
                TurnNumber = options.TurnNumber,
                ContactWeights = options.ContactWeights,
                MaxCandidates = options.MaxCandidates,
            }, board);
            return [.. evalResult];
        }

        // <inheritdoc />
        public async Task<FinalEvalResultModels> EvalMoveSequenceCandidatesAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.BoardContract);
            ArgumentNullException.ThrowIfNull(options.Candidates);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);
            ArgumentNullException.ThrowIfNull(options.ContactWeights);
            ArgumentNullException.ThrowIfNull(options.BotLevel);
            ArgumentNullException.ThrowIfNull(options.IsWhite);

            if (options.Candidates.Count == 0)
                return [];

            var board = BoardService.CreateBoard(options.BoardContract);
            var evalResult = await GetCandidatesByEvalAsync(options, board);

            return [.. evalResult];
        }

        // <inheritdoc />
        public NormalizedEvalResultModel EvalPositionForTraining(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.BoardContract);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);
            ArgumentNullException.ThrowIfNull(options.IsWhite);
            var board = BoardService.CreateBoard(options.BoardContract);
            var eval = CalculateEvalModelWithDiagnostics(board, options.TurnNumber.Value, options.IsWhite.Value);
            return NormalizedEvalResultModel.From(eval);
        }

        // <inheritdoc />
        public async Task<FinalEvalResultModel> EvalMoveSequenceAsync(FeatureEvalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.Candidate);
            var evalResult = await EvalMoveSequenceCandidatesAsync(new FeatureEvalOptions
            {
                BoardContract = options.BoardContract,
                IsWhite = options.IsWhite,
                Candidates = [options.Candidate],
                BotLevel = options.BotLevel,
                TurnNumber = options.TurnNumber,
                ContactWeights = options.ContactWeights,
            });
            return evalResult[0];
        }

        internal async Task<IEnumerable<FinalEvalResultModel>> GetCandidatesByEvalAsync(FeatureEvalOptions options, IBoardModel board)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(options.BotLevel);
            ArgumentNullException.ThrowIfNull(options.TurnNumber);
            ArgumentNullException.ThrowIfNull(options.Candidates);
            ArgumentNullException.ThrowIfNull(options.ContactWeights);
            ArgumentNullException.ThrowIfNull(options.IsWhite);

            var evalCount = Math.Min(options.MaxCandidates ?? options.Candidates.Count, options.Candidates.Count);

            if (evalCount <= 0)
            {
                return [];
            }

            var evals = new FinalEvalResultModel[evalCount];
            var searchDepth = options.BotLevel.Value.ToSearchDepth();
            var turnNumber = options.TurnNumber.Value;
            var isWhite = options.IsWhite.Value;

            if (ParallelCandidateCount == 1)
            {
                // we want to process the candidates sequentially
                for (var index = 0; index < evalCount; index++)
                {
                    await EvaluateCandidateAsync(board, index);
                }
            }
            else
            {
                // we want to process the candidates in parallel
                var next = -1;
                var failed = 0;
                var workers = new Task[Math.Min(ParallelCandidateCount, evalCount)];
                // we start workers until parallel candidate count is reached
                for (var index = 0; index < workers.Length; index++)
                {
                    workers[index] = RunAsync();
                }

                // we expect whenAll to observes every producer, including failures during shutdown.
                await Task.WhenAll(workers);

                async Task RunAsync()
                {
                    try
                    {
                        while (Volatile.Read(ref failed) == 0)
                        {
                            var index = Interlocked.Increment(ref next);
                            if (index >= evalCount)
                                return;
                            await EvaluateCandidateAsync(board.DeepClone(), index);
                        }
                    }
                    catch
                    {
                        Interlocked.Exchange(ref failed, 1);
                        throw;
                    }
                }
            }

            // Stable sorting preserves original candidate order for equal evals.
            return evals.OrderByDescending(e => e.Equity);

            async Task EvaluateCandidateAsync(IBoardModel ownedBoard, int index)
            {
                var moveSeq = options.Candidates[index];
                var appliedMoves = 0;
                EvaluationMetrics.RecordActiveCandidates(1);
                try
                {
                    // we iterate over all possible moves for the active player
                    foreach (var move in moveSeq.Moves)
                    {
                        BoardService.MoveCheckerTo(ownedBoard, move.From, move.To, isWhite);
                        appliedMoves++;
                    }

                    var eval = CalculateEvalModelWithDiagnostics(ownedBoard, turnNumber, isWhite);
                    var evalModel = NormalizedEvalResultModel.From(eval);
                    EvalResult evalResult;

                    if (searchDepth == SearchDepth.TwoPly && _neuralEvalService != null)
                    {
                        // we calculate the equity based on a two-ply evaluation of the resulting board state
                        evalResult = await CalculateTwoPlyEquityAsync(ownedBoard, turnNumber, isWhite);
                    }
                    else
                    {
                        // we calculate the one-ply equity for the active player and his move
                        evalResult = await EvalPositionAsync(ownedBoard, isWhite, eval, options.ContactWeights);
                    }

                    evals[index] = new FinalEvalResultModel(
                        evalResult.Equity,
                        evalResult.Prediction,
                        moveSeq,
                        evalModel);
                }
                finally
                {
                    try
                    {
                        for (var moveIndex = appliedMoves - 1; moveIndex >= 0; moveIndex--)
                            BoardService.UndoMove(ownedBoard, moveSeq.Moves[moveIndex], isWhite);
                    }
                    finally
                    {
                        EvaluationMetrics.RecordActiveCandidates(-1);
                    }
                }
            }
        }

        private Task<EvalResult> CalculateTwoPlyEquityAsync(IBoardModel board, int turnNumber, bool isWhite)
        {
            if (_neuralEvalService == null)
            {
                throw new InvalidOperationException("Neural evaluation service is not available for two-ply search.");
            }

            // we check if the game has already ended and return the terminal equity if so
            if (TryGetTerminal(board, isWhite, out var terminalEquity))
            {
                return Task.FromResult(terminalEquity);
            }

            var evaluator = new TwoPlySearchEvaluator(
                BoardService,
                // we pass the position eval calculation as a func
                (boards, perspectiveIsWhite) =>
                {
                    // we increase the turn number for the next ply, since the opponent will have played a move
                    var twoPlyTurnNumber = turnNumber + 1;
                    return CalculatePositionBatchAsync(boards, _neuralEvalService, twoPlyTurnNumber, perspectiveIsWhite);
                },
                // Bound each callers work item even when the shared worker allows larger batches.
                maxBatchSize: Math.Min(_neuralEvalService.MaxBatchSize, 32),
                batchAcrossRolls: BatchAcrossRolls);

            if (ReuseReplyBoard)
            {
                var buffers = ReuseFeatureBuffers ? new float[Math.Min(_neuralEvalService.MaxBatchSize, 32)][] : null;
                var nextBuffer = 0;
                // we reuse the same board instance for each inference reply, applying and undoing the moves before inference.
                return evaluator.EvalPreparedAsync(board, isWhite,
                    (position, isWhite) =>
                    {
                        // we increase the turn number for the next ply, since the opponent will have played a move
                        var twoPlyTurnNumber = turnNumber + 1;
                        var prepared = PreparePosition(position, _neuralEvalService, buffers, nextBuffer, twoPlyTurnNumber, isWhite);
                        nextBuffer++;
                        return prepared;
                    }, async positions =>
                    {
                        var results = await EvalPreparedPositionsAsync(positions, board.Modus, _neuralEvalService);
                        nextBuffer = 0;
                        return results;
                    });
            }

            // we create board deep clones for each board inference
            return evaluator.EvaluateAsync(board, isWhite);
        }

        private PreparedPosition PreparePosition(
            IBoardModel board,
            INeuralEvalService neural,
            float[][]? buffers,
            int bufferIndex,
            int turnNumber,
            bool isWhite)
        {
            if (TryGetTerminal(board, isWhite, out var terminal))
            {
                return new PreparedPosition(null, terminal);
            }

            var eval = CalculateEvalModelWithDiagnostics(board, turnNumber, isWhite);
            var normalizedEval = NormalizedEvalResultModel.From(eval);

            // we extract the feature vectors into the buffer if available, otherwise we allocate a new array
            var featureVectors = buffers == null
                ? neural.ExtractFeatures(normalizedEval, board, isWhite)
                : buffers[bufferIndex] = neural.ExtractFeatures(normalizedEval, board, isWhite, buffers[bufferIndex]);

            return new PreparedPosition(featureVectors, null);
        }

        private static async Task<EvalResult[]> EvalPreparedPositionsAsync(IReadOnlyList<PreparedPosition> positions, GameModus modus, INeuralEvalService neural)
        {
            var evalResults = new EvalResult[positions.Count];
            var features = new List<float[]>(positions.Count);
            var indices = new List<int>(positions.Count);

            for (var index = 0; index < positions.Count; index++)
            {
                var position = positions[index];
                if (position.Features == null)
                {
                    evalResults[index] = position.Terminal ?? EvalResult.Inconclusive();
                }
                else
                {
                    features.Add(position.Features);
                    indices.Add(index);
                }
            }

            if (features.Count == 0)
            {
                return evalResults;
            }

            var predictions = await neural.PredictBatchAsync(features.ToArray());

            if (predictions.Length != indices.Count)
            {
                throw new InvalidDataException("Batch inference must return one result per non-terminal reply.");
            }

            for (var row = 0; row < indices.Count; row++)
            {
                evalResults[indices[row]] = ToEvalResult(modus, predictions[row]);
            }

            return evalResults;
        }

        private async Task<EvalResult> EvalPositionAsync(IBoardModel board, bool isWhite, EvalResultModel eval, ContactWeightModel contactWeights)
        {
            // we check if the game has already ended and return the terminal eval if so
            if (TryGetTerminal(board, isWhite, out var terminal))
            {
                return terminal;
            }

            // we return score based on linear weights
            if (_neuralEvalService == null)
            {
                // we use a linear score as equity for non-inference evals
                var score = EvalScoreCalculator.CalculateScore(eval, contactWeights);
                return new EvalResult(score, null);
            }

            var evalModel = NormalizedEvalResultModel.From(eval);
            // we calculate the nn model prediction
            var predictions = await _neuralEvalService.PredictAsync(evalModel, board, isWhite);

            return ToEvalResult(board.Modus, predictions);
        }

        private async Task<EvalResult[]> CalculatePositionBatchAsync(
            IReadOnlyList<IBoardModel> boards,
            INeuralEvalService neuralService,
            int turnNumber,
            bool isWhite)
        {
            var evalResults = new EvalResult[boards.Count];
            var features = new List<float[]>(boards.Count);
            var indices = new List<int>(boards.Count);
            // we collect all feature vectors from all different board positions that are not terminal
            for (var index = 0; index < boards.Count; index++)
            {
                var board = boards[index];
                if (TryGetTerminal(board, isWhite, out var terminal))
                {
                    evalResults[index] = terminal;
                    continue;
                }
                var eval = CalculateEvalModelWithDiagnostics(board, turnNumber, isWhite);
                features.Add(neuralService.ExtractFeatures(NormalizedEvalResultModel.From(eval), board, isWhite));
                indices.Add(index);
            }

            if (features.Count == 0)
            {
                return evalResults;
            }

            float[][] predictions;
            EvaluationMetrics.RecordFeatureRows(features.Count);

            try
            {
                predictions = await neuralService.PredictBatchAsync(features.ToArray());
            }
            finally
            {
                EvaluationMetrics.RecordFeatureRows(-features.Count);
            }

            if (predictions.Length != indices.Count)
            {
                throw new InvalidDataException("Batch inference must return one result per non-terminal reply.");
            }

            for (var row = 0; row < indices.Count; row++)
            {
                evalResults[indices[row]] = ToEvalResult(boards[indices[row]].Modus, predictions[row]);
            }

            return evalResults;
        }

        private static EvalResult ToEvalResult(GameModus modus, float[] predictions)
        {
            // TODO: support 5 head output for Plakoto and Fevga
            if (modus == GameModus.Plakoto || modus == GameModus.Fevga)
            {
                // we currently have no proper way to calculate equity for plakoto or fevga
                var falseEquity = predictions[0];
                return new EvalResult(falseEquity, predictions);
            }
            var equity = GameEquityModel.CalculateEquity(predictions);
            return new EvalResult(equity, predictions);
        }

        private static bool TryGetTerminal(IBoardModel board, bool isWhite, out EvalResult terminal)
        {
            // some game modes allow a tie
            if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
            {
                terminal = EvalResult.Inconclusive();
                return true;
            }

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var blackWon = board.BearOffCountBlack == board.WinConditionCount;

            if (!whiteWon && !blackWon)
            {
                terminal = EvalResult.Inconclusive();
                return false;
            }

            var result = board.ToGameResult(Guid.Empty, whiteWon);
            var playerResult = isWhite == whiteWon ? result.WinnerResult : result.LoserResult;
            terminal = playerResult switch
            {
                GameResult.Single => new EvalResult(1d, [1f, 0f, 0f, 0f, 0f]),
                GameResult.Gammon => new EvalResult(2d, [1f, 1f, 0f, 0f, 0f]),
                GameResult.Backgammon => new EvalResult(3d, [1f, 1f, 1f, 0f, 0f]),
                GameResult.LostSingle => new EvalResult(-1d, [0f, 0f, 0f, 0f, 0f]),
                GameResult.LostGammon => new EvalResult(-2d, [0f, 0f, 0f, 1f, 0f]),
                GameResult.LostBackgammon => new EvalResult(-3d, [0f, 0f, 0f, 1f, 1f]),
                // ReSharper disable once RedundantSwitchExpressionArms
                GameResult.Draw => EvalResult.Inconclusive(),
                _ => EvalResult.Inconclusive()
            };
            return true;
        }

        private EvalResultModel CalculateEvalModelWithDiagnostics(IBoardModel board, int turnNumber, bool isWhite)
        {
            using var measurement = EvaluationMetrics.Measure(EvaluationMetrics.Features);
            return CalculateEvalModel(board, turnNumber, isWhite);
        }

        /// <summary>
        /// Calculates the game modus specific evaluation model for the given board state and player perspective.
        /// </summary>
        /// <param name="board">The current board state.</param>
        /// <param name="isWhite">Indicates if the perspective is for the white player.</param>
        /// <param name="turnNumber">The current turn number.</param>
        /// <returns>The evaluation result model.</returns>
        protected abstract EvalResultModel CalculateEvalModel(IBoardModel board, int turnNumber, bool isWhite);
    }

    public readonly struct EvalResult
    {
        public double Equity { get; }

        public float[]? Prediction { get; }

        public EvalResult(double equity, float[]? prediction)
        {
            Equity = equity;
            Prediction = prediction;
        }

        public static EvalResult Inconclusive()
        {
            return new EvalResult(0d, null);
        }
    }
}
