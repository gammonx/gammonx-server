using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public abstract class BaseFeatureEvalServiceImpl : IFeatureEvalService
    {
        private readonly INeuralEvalService _neuralEvalService;

        protected abstract IBoardService BoardService { get; }

        protected RaceFeature RaceFeature { get; } = new();

        protected BaseFeatureEvalServiceImpl(INeuralEvalService neuralEvalService)
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
                var isRace = RaceFeature.Eval(board, isWhite);
                var eval = CalculateEvalModel(board, isWhite, isRace);

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

            var isRace = RaceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);

            double score;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_neuralEvalService != null)
            {
                var predictions = _neuralEvalService.Predict(NormalizedEvalResultModel.From(eval), board, isWhite);
                if (board.Modus == GameModus.Plakoto || board.Modus == GameModus.Fevga)
                {
                    // TODO: enable full GAME equity predictions for plakoto/fevga
                    // we just return pure single win probability for now
                    score = predictions[0];
                }
                else
                {
                    // we calculate game equity by outcome probabilities
                    var outcome = new GameOutcomeModel(predictions);
                    var equityModel = new GameEquityModel(outcome);
                    score = equityModel.Equity;
                }
            }
            else
            {
                score = EvalScoreCalculator.CalculateScore(eval, contactWeights);
            }

            return score;
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
            var legalMovesSeq = BoardService.GetLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return new FinalEvalResultModels();

            var evalCount = Math.Min(maxCandidates ?? legalMovesSeq.Length, legalMovesSeq.Length);
            var evalResult = GetCandidatesByEval(board, legalMovesSeq, isWhite, contactWeights, evalCount);
            return new FinalEvalResultModels(evalResult);
        }

        // <inheritdoc />
        public NormalizedEvalResultModel EvalPositionForTraining(BoardModelContract boardContract, bool isWhite)
        {
            var board = BoardService.CreateBoard(boardContract);
            var isRace = RaceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);
            return NormalizedEvalResultModel.From(eval);
        }

        // <inheritdoc />
        public FinalEvalResultModel EvalMoveSequence(BoardModelContract contract, bool isWhite, MoveSequenceModel moveSequence, ContactWeightModel contactWeights)
        {
            var board = BoardService.CreateBoard(contract);
            var moveSequences = new[] { moveSequence };
            const int evalCount = 1;
            var evalResult = GetCandidatesByEval(board, moveSequences, isWhite, contactWeights, evalCount);
            return evalResult.First();
        }

        private IEnumerable<FinalEvalResultModel> GetCandidatesByEval(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ContactWeightModel contactWeights,
            int evalCount)
        {
            var evals = new List<FinalEvalResultModel>();

            for (var idx = 0; idx < evalCount; idx++)
            {
                var moveSeq = legalMovesSeq[idx];

                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                var eval = CalculateEvalModel(board, isWhite, false);
                var evalModel = NormalizedEvalResultModel.From(eval);
                double score;
                if (_neuralEvalService != null)
                {
                    var predictions = _neuralEvalService.Predict(evalModel, board, isWhite);
                    if (board.Modus == GameModus.Plakoto || board.Modus == GameModus.Fevga)
                    {
                        // TODO: enable full GAME equity predictions for plakoto/fevga
                        // we just return pure single win probability for now
                        score = predictions[0];
                    }
                    else
                    {
                        // we calculate game equity by outcome probabilities
                        var outcome = new GameOutcomeModel(predictions);
                        var equityModel = new GameEquityModel(outcome);
                        score = equityModel.Equity;
                    }
                }
                else
                {
                    // we calculate score by linear weighting model
                    score = EvalScoreCalculator.CalculateScore(eval, contactWeights);
                }

                var reversedMoveSeq = moveSeq.DeepClone();
                reversedMoveSeq.Moves.Reverse();
                foreach (var undoMove in reversedMoveSeq.Moves)
                {
                    // we manually undo the moves in order to reduce instance allocations
                    BoardService.UndoMove(board, undoMove, isWhite);
                }

                var evalResult = new FinalEvalResultModel(score, moveSeq, evalModel);
                evals.Add(evalResult);
            }

            return evals.OrderByDescending(e => e.Score);
        }

        protected abstract EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace);
    }
}
