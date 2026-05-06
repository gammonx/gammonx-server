using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;

using System.Buffers;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public abstract class BaseFeatureEvalServiceImpl : IFeatureEvalService
    {
        private readonly INeuralEvalService _neuralEvalService;

        protected BaseFeatureEvalServiceImpl(INeuralEvalService neuralEvalService)
        {
            _neuralEvalService = neuralEvalService;
        }

        protected abstract IBoardService BoardService { get; }

        protected RaceFeature RaceFeature { get; } = new();

        // <inheritdoc />
        public double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            var isRace = RaceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);

            var score = _neuralEvalService?.Predict(NormalizedEvalResultModel.From(eval)) ?? EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
            return score;
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequence(EvalMoveRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int maxCandidates)
        {
            return EvalMoveSequenceForTraining(contract, cheapContactWeights, contactWeights, raceWeights, maxCandidates).BestMove;
        }

        // <inheritdoc />
        public FinalEvalResult EvalMoveSequenceForTraining(EvalMoveRequestContract contract, ContactWeightModel cheapContactWeights, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int maxCandidates)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = BoardService.CreateBoard(boardContract);
            var legalMovesSeq = BoardService.GetLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return new FinalEvalResult(new MoveSequenceModel(), default);

            // we first compute cheap features to rank candidates, avoiding the expensive
            // e.g. ContactProbabilityFeature which internally explores all 21 dice combinations.
            var pool = ArrayPool<CheapEvalResult>.Shared;
            var legalMovesCopy = legalMovesSeq.Select(lms => lms.DeepClone()).ToArray();
            var candidates = GetCandidatesByCheapScore(board, legalMovesCopy, isWhite, cheapContactWeights, raceWeights, pool);
            try
            {
                var identicalTopEvalCandidates = candidates.Count(c => Math.Abs(c.CheapScore - candidates[0].CheapScore) < 1e-9);
                // we want at least all candidates with the same cheap score to be fully evaluated
                // in this case we overwrite the given maxCandidates count
                var evalCount = Math.Min(Math.Max(maxCandidates, identicalTopEvalCandidates), candidates.Count);

                var evalResult = GetCandidateByFullEval(board, legalMovesSeq, isWhite, candidates, contactWeights, raceWeights, evalCount);
                return evalResult;
            }
            finally
            {
                pool.Return(candidates.Array!);
            }
        }

        private FinalEvalResult GetCandidateByFullEval
            (IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ArraySegment<CheapEvalResult> candidates,
            ContactWeightModel contactWeights,
            RaceWeightModel raceWeights,
            int evalCount)
        {
            var bestScore = double.MinValue;
            var bestMoveSeq = legalMovesSeq[0];
            var bestFeatures = default(NormalizedEvalResultModel);

            for (int i = 0; i < evalCount; i++)
            {
                var idx = candidates[i].Index;
                var moveSeq = legalMovesSeq[idx];

                if (candidates[i].IsRace)
                {
                    // race score were already calculated
                    if (candidates[i].CheapScore > bestScore)
                    {
                        bestScore = candidates[i].CheapScore;
                        bestMoveSeq = moveSeq;
                    }
                    continue;
                }

                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                // TODO: create board hash, check if already computed

                // we now calculate the more expensive contact features
                var eval = CalculateEvalModel(board, isWhite, false);
                var normalizedEval = NormalizedEvalResultModel.From(eval);
                var score = _neuralEvalService?.Predict(normalizedEval) ?? EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);

                moveSeq.Moves.Reverse();
                foreach (var undoMove in moveSeq.Moves)
                {
                    // we manually undo the moves in order to reduce instance allocations
                    BoardService.UndoMove(board, undoMove, isWhite);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveSeq = moveSeq;
                    // we capture the features of the resulting board, this is the NN training input
                    bestFeatures = normalizedEval;
                }
            }

            return new FinalEvalResult(bestMoveSeq, bestFeatures);
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

                // TODO: create board hash, check if already computed

                var isRace = RaceFeature.Eval(board, isWhite);
                var eval = CalculateCheapEvalModel(board, isWhite, isRace);

                moveSeq.Moves.Reverse();
                foreach (var undoMove in moveSeq.Moves)
                {
                    // we manually undo the moves in order to reduce instance allocations
                    BoardService.UndoMove(board, undoMove, isWhite);
                }

                var cheapScore = EvalScoreCalculator.CalculateCheapScore(eval, cheapContactWeights, raceWeights);
                var cheapEvalResult = new CheapEvalResult(cheapScore, index, isRace);
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
