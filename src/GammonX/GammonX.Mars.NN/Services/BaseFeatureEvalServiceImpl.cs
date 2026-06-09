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
        public CubeAction EvalCube(EvalCubeRequestContract contract)
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

                // we calculate current match equity
                var noDouble = MatchEquityCalculator.CalculateEquity(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeModel.DoublingCubeValue);
                // we calculate the match equity if double is passed
                var doublePass = MatchEquityCalculator.CalculateEquity(
                    equityModel,
                    contract.PointsAwayPlayer - cubeModel.DoublingCubeValue,
                    contract.PointsAwayOpp,
                    cubeModel.DoublingCubeValue);
                // we calculate match equity if double is accepted
                var doubleTake = MatchEquityCalculator.CalculateEquity(
                    equityModel,
                    contract.PointsAwayPlayer,
                    contract.PointsAwayOpp,
                    cubeModel.DoublingCubeValue * 2);

                if (doubleTake <= noDouble)
                {
                    // we are losing equity by offering a double if opponent takes or passes
                    return CubeAction.NoDouble;
                }

                var volatility = Math.Abs(equityModel.WinSingleP - 0.5) + equityModel.WinGammonP + equityModel.LoseGammonP;

                if (doublePass >= doubleTake && volatility > 0.2)
                {
                    // we expect the opponent to pass if we double, and we have a strong enough board
                    // to win by a gammon or backgammon.
                    return CubeAction.TooGood;
                }

                return CubeAction.Double;
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

            if (evalMoves.Length != 0)
            {
                return evalMoves.First().Move;
            }

            return new MoveSequenceModel();
        }

        // <inheritdoc />
        public FinalEvalResult[] EvalMoveSequenceForTraining(EvalMoveRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int maxCandidates)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = BoardService.CreateBoard(boardContract);
            var legalMovesSeq = BoardService.GetLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return [];

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
                return evalResult.ToArray();
            }
            finally
            {
                pool.Return(candidates.Array!);
            }
        }

        private IEnumerable<FinalEvalResult> GetCandidatesByFullEval(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ArraySegment<CheapEvalResult> candidates,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            int evalCount)
        {
            var evals = new List<FinalEvalResult>();

            for (int i = 0; i < evalCount; i++)
            {
                var idx = candidates[i].Index;
                var moveSeq = legalMovesSeq[idx];

                if (candidates[i].IsRace)
                {
                    // race score were already calculated
                    if (evals.Count == 0 || candidates[i].CheapScore > evals[0].Score)
                    {
                        var cheapEvalResultResult = new FinalEvalResult(candidates[i].CheapScore, moveSeq, candidates[i].EvalResult);
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

                var evalResult = new FinalEvalResult(score, moveSeq, evalModel);
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
