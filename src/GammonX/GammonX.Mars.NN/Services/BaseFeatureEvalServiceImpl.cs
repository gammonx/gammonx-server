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
        public bool EvalCube(EvalCubeRequestContract contract, ContactWeightModel cheapContactWeight, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var boardContract = contract.Board;
            var board = BoardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            if (board is IDoublingCubeModel cubeModel)
            {
                return false;
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

            double pWin;
            if (_neuralEvalService != null)
            {
                var predictions = _neuralEvalService.Predict(NormalizedEvalResultModel.From(eval), board, isWhite);
                pWin = predictions[0];
            }
            else
            {
                pWin = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
            }
            
            return pWin;
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

            // we first compute cheap features to rank candidates, avoiding the expensive
            // e.g. ContactProbabilityFeature which internally explores all 21 dice combinations.
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

        private IEnumerable<FinalEvalResult> GetCandidatesByFullEval
            (IBoardModel board,
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
                double pWin;
                if (_neuralEvalService != null)
                {
                    var predictions = _neuralEvalService.Predict(evalModel, board, isWhite);
                    pWin = predictions[0];
                }
                else
                {
                    pWin = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
                }

                var reversedMoveSeq = moveSeq.DeepClone();
                reversedMoveSeq.Moves.Reverse();
                foreach (var undoMove in reversedMoveSeq.Moves)
                {
                    // we manually undo the moves in order to reduce instance allocations
                    BoardService.UndoMove(board, undoMove, isWhite);
                }

                var evalResult = new FinalEvalResult(pWin, moveSeq, evalModel);
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
