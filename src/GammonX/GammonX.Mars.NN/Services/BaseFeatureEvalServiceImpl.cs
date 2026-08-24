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

        protected abstract IBoardService BoardService { get; }

        protected BaseFeatureEvalServiceImpl(INeuralEvalService? neuralEvalService)
        {
            _neuralEvalService = neuralEvalService;
        }

        // <inheritdoc />
        public (CubeAction ShouldOffer, CubeAction ShouldTake) EvalCube(EvalCubeRequestContract contract)
        {
            if (_neuralEvalService == null)
                throw new InvalidOperationException("Neural evaluation service is required for cube evaluation.");

            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            if (board is IDoublingCubeModel cubeModel)
            {
                var eval = CalculateEvalModel(board, isWhite);

                var predictions = _neuralEvalService.Predict(NormalizedEvalResultModel.From(eval), board, isWhite);
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
                // match equity if double is offered and accepted (game continues at doubled cube)
                var doubleTake = MatchEquityCalculator.CalculateEquity(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeValue * 2);

                CubeAction shouldOffer;
                CubeAction shouldTake;

                // we expect the opponent takes if their equity from taking >= their equity from passing:
                if (doubleTake <= equityIfOppPasses)
                {
                    // we expect opponent would take, players equity after doubling = doubleTake
                    shouldOffer = doubleTake > noDouble ? CubeAction.Double : CubeAction.NoDouble;
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

                // we expect the player takes if their equity from taking >= their equity from passing
                if (doubleTake >= equityIfPlayerPasses)
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
        public double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel contactWeights)
        {
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            var eval = CalculateEvalModel(board, isWhite);
            return CalculatePositionScore(board, isWhite, eval, contactWeights);
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequences(EvalMoveRequestContract contract, ContactWeightModel contactWeights, int? maxCandidates = null)
        {
            var evalMoves = EvalMoveSequencesForTraining(contract, contactWeights, maxCandidates);
            return evalMoves.Select(contract.BotLevel);
        }

        // <inheritdoc />
        public FinalEvalResultModels EvalMoveSequencesForTraining(EvalMoveRequestContract contract, ContactWeightModel contactWeights, int? maxCandidates = null)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = BoardService.CreateBoard(boardContract);
            // we only evaluate move sequences which result in a unique end board state
            var legalMovesSeq = BoardService.GetUniqueLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return [];

            var evalCount = Math.Min(maxCandidates ?? legalMovesSeq.Length, legalMovesSeq.Length);
            var evalResult = GetCandidatesByEval(board, legalMovesSeq, isWhite, contactWeights, evalCount, contract.BotLevel);
            return [.. evalResult];
        }

        // <inheritdoc />
        public NormalizedEvalResultModel EvalPositionForTraining(BoardModelContract boardContract, bool isWhite)
        {
            var board = BoardService.CreateBoard(boardContract);
            var eval = CalculateEvalModel(board, isWhite);
            return NormalizedEvalResultModel.From(eval);
        }

        // <inheritdoc />
        public FinalEvalResultModel EvalMoveSequence(BoardModelContract contract, bool isWhite, MoveSequenceModel moveSequence, ContactWeightModel contactWeights)
        {
            var board = BoardService.CreateBoard(contract);
            var moveSequences = new[] { moveSequence };
            const int evalCount = 1;
            var evalResult = GetCandidatesByEval(board, moveSequences, isWhite, contactWeights, evalCount, BotLevel.TwoPly);
            return evalResult.First();
        }

        private IEnumerable<FinalEvalResultModel> GetCandidatesByEval(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ContactWeightModel contactWeights,
            int evalCount,
            BotLevel botLevel)
        {
            var evals = new List<FinalEvalResultModel>();

            for (var idx = 0; idx < evalCount; idx++)
            {
                var moveSeq = legalMovesSeq[idx];

                var appliedMoves = 0;
                try
                {
                    // we iterate over all possible moves for the active player
                    foreach (var move in moveSeq.Moves)
                    {
                        BoardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                        appliedMoves++;
                    }

                    var eval = CalculateEvalModel(board, isWhite);
                    var evalModel = NormalizedEvalResultModel.From(eval);
                    var score = 0d;

                    if (botLevel == BotLevel.TwoPly && _neuralEvalService != null)
                    {
                        // we calculate the score based on a two-ply evaluation of the resulting board state
                        score = CalculateTwoPlyScore(board, isWhite, contactWeights);
                    }
                    else
                    {
                        // we calculate the one-ply score for the active player and his move
                        score = CalculatePositionScore(board, isWhite, eval, contactWeights);
                    }

                    evals.Add(new FinalEvalResultModel(score, moveSeq, evalModel));
                }
                finally
                {
                    // we undo all moves to restore the board state for the next evaluation
                    // we do this in order to avoid allocating board copies for each move sequence, which would be expensive
                    for (var moveIndex = appliedMoves - 1; moveIndex >= 0; moveIndex--)
                    {
                        BoardService.UndoMove(board, moveSeq.Moves[moveIndex], isWhite);
                    }
                }
            }

            return evals.OrderByDescending(e => e.Score);
        }

        private double CalculateTwoPlyScore(IBoardModel board, bool isWhite, ContactWeightModel contactWeights)
        {
            // we check if the game has already ended and return the terminal score if so
            if (TryGetTerminalScore(board, isWhite, out var terminalScore))
            {
                return terminalScore;
            }

            var evaluator = new TwoPlySearchEvaluator(
                BoardService,
                // we pass the position score calculation as a func
                (position, perspectiveIsWhite) =>
                {
                    var eval = CalculateEvalModel(position, perspectiveIsWhite);
                    return CalculatePositionScore(position, perspectiveIsWhite, eval, contactWeights);
                });

            return evaluator.Evaluate(board, isWhite);
        }

        private double CalculatePositionScore(IBoardModel board, bool isWhite, EvalResultModel eval, ContactWeightModel contactWeights)
        {
            // we check if the game has already ended and return the terminal score if so
            if (TryGetTerminalScore(board, isWhite, out var terminalScore))
            {
                return terminalScore;
            }

            // we return score based on linear weights
            if (_neuralEvalService == null)
            {
                return EvalScoreCalculator.CalculateScore(eval, contactWeights);
            }

            var evalModel = NormalizedEvalResultModel.From(eval);
            // we calculate the nn model prediction
            var predictions = _neuralEvalService.Predict(evalModel, board, isWhite);

            // TODO: support 5 head output for Plakoto and Fevga
            if (board.Modus == GameModus.Plakoto || board.Modus == GameModus.Fevga)
            {
                return predictions[0];
            }

            return new GameEquityModel(new GameOutcomeModel(predictions)).Equity;
        }

        private static bool TryGetTerminalScore(IBoardModel board, bool isWhite, out double score)
        {
            // some game modes allow a tie
            if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
            {
                score = 0d;
                return true;
            }

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var blackWon = board.BearOffCountBlack == board.WinConditionCount;

            if (!whiteWon && !blackWon)
            {
                score = 0d;
                return false;
            }

            var result = board.ToGameResult(Guid.Empty, whiteWon);
            var playerResult = isWhite == whiteWon ? result.WinnerResult : result.LoserResult;
            score = playerResult switch
            {
                GameResult.Single => 1d,
                GameResult.Gammon => 2d,
                GameResult.Backgammon => 3d,
                GameResult.LostSingle => -1d,
                GameResult.LostGammon => -2d,
                GameResult.LostBackgammon => -3d,
                GameResult.Draw => 0d,
                _ => 0d
            };
            return true;
        }

        /// <summary>
        /// Calculates the game modus specific evaluation model for the given board state and player perspective.
        /// </summary>
        /// <param name="board">The current board state.</param>
        /// <param name="isWhite">Indicates if the perspective is for the white player.</param>
        /// <returns>The evaluation result model.</returns>
        protected abstract EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite);
    }
}
