using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using System.Buffers;

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

            if (board is IDoublingCubeModel cubeModel && _neuralEvalService != null)
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
        public double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            var isRace = RaceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);

            double score;
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
                score = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
            }

            return score;
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequence(EvalMoveRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int maxCandidates)
        {
            var evalMoves = EvalMoveSequenceForTraining(contract, cheapContactWeights, contactWeights, raceWeights, maxCandidates);
            return evalMoves.Select(contract.BotLevel);
        }

        // <inheritdoc />
        public FinalEvalResultModels EvalMoveSequenceForTraining(EvalMoveRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int maxCandidates)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = BoardService.CreateBoard(boardContract);
            var legalMovesSeq = BoardService.GetLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return new FinalEvalResultModels();

            // we first compute features based on linear weighting to rank candidates
            var pool = ArrayPool<CheapEvalResult>.Shared;
            var candidates = GetCandidatesByCheapScore(board, legalMovesSeq, isWhite, cheapContactWeights, raceWeights, pool);
            try
            {
                var identicalTopEvalCandidates = candidates.Count(c => Math.Abs(c.CheapScore - candidates[0].CheapScore) < 1e-9);
                // we want at least all candidates with the same cheap score to be fully evaluated
                // in this case we overwrite the given maxCandidates count
                var evalCount = Math.Min(Math.Max(maxCandidates, identicalTopEvalCandidates), candidates.Count);

                var evalResult = GetCandidatesByFullEval(board, legalMovesSeq, isWhite, candidates, contactWeights, raceWeights, evalCount);
                return new FinalEvalResultModels(evalResult);
            }
            finally
            {
                pool.Return(candidates.Array!);
            }
        }

        private IEnumerable<FinalEvalResultModel> GetCandidatesByFullEval(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ArraySegment<CheapEvalResult> candidates,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            int evalCount)
        {
            var evals = new List<FinalEvalResultModel>();

            for (int i = 0; i < evalCount; i++)
            {
                var idx = candidates[i].Index;
                var moveSeq = legalMovesSeq[idx];

                if (candidates[i].IsRace)
                {
                    // race score were already calculated
                    if (evals.Count == 0 || candidates[i].CheapScore > evals[0].Score)
                    {
                        var cheapEvalResultResult = new FinalEvalResultModel(candidates[i].CheapScore, moveSeq, candidates[i].EvalResult);
                        evals.Add(cheapEvalResultResult);
                    }
                    continue;
                }

                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                // we now calculate the more expensive contact features
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
                    score = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
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

        private ArraySegment<CheapEvalResult> GetCandidatesByCheapScore(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights,
            ArrayPool<CheapEvalResult> pool)
        {
            var buffer = pool.Rent(legalMovesSeq.Length);
            for (int index = 0; index < legalMovesSeq.Length; index++)
            {
                var moveSeq = legalMovesSeq[index];
                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                var isRace = RaceFeature.Eval(board, isWhite);
                var eval = CalculateCheapEvalModel(board, isWhite, isRace);

                var reversedMoveSeq = moveSeq.DeepClone();
                reversedMoveSeq.Moves.Reverse();
                foreach (var undoMove in reversedMoveSeq.Moves)
                {
                    // we manually undo the moves in order to reduce instance allocations
                    BoardService.UndoMove(board, undoMove, isWhite);
                }

                var cheapScore = EvalScoreCalculator.CalculateCheapScore(eval, cheapContactWeights, raceWeights);
                var cheapEvalResult = new CheapEvalResult(cheapScore.Item2, index, isRace, cheapScore.Item1);
                buffer[index] = cheapEvalResult;
            }

            // we sort by cheap score descending and only fully evaluate the top N contact candidates.
            Array.Sort(buffer, 0, legalMovesSeq.Length, CheapEvalResult.DescendingComparer.Instance);
            return new ArraySegment<CheapEvalResult>(buffer, 0, legalMovesSeq.Length);
        }

        protected abstract EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace);

        protected abstract EvalResultModel CalculateCheapEvalModel(IBoardModel board, bool isWhite, bool isRace);
    }
}
