using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Contracts;

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
            var candidates = GetCandidatesByCheapScore(board, legalMovesSeq, isWhite, cheapContactWeights, raceWeights);

            var identicalTopEvalCandidates = candidates.Count(c => Math.Abs(c.CheapScore - candidates[0].CheapScore) < 1e-9);
            // we want at least all candidates with the same cheap score to be fully evaluated
            // in this case we overwrite the given maxCandidates count
            var evalCount = Math.Min(Math.Max(maxCandidates, identicalTopEvalCandidates), candidates.Length);

            var evalResult = GetCandidateByFullEval(board, legalMovesSeq, isWhite, candidates, contactWeights, raceWeights, evalCount);
            return evalResult;
        }

        private FinalEvalResult GetCandidateByFullEval(IBoardModel board, MoveSequenceModel[] legalMovesSeq, bool isWhite, CheapEvalResult[] candidates, ContactWeightModel contactWeights, RaceWeightModel raceWeights, int evalCount)
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
                    // race score are already calculated
                    if (candidates[i].CheapScore > bestScore)
                    {
                        bestScore = candidates[i].CheapScore;
                        bestMoveSeq = moveSeq;
                    }
                    continue;
                }

                var shadowBoard = board.DeepClone();
                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);
                }

                // we now calculate the more expensive contact features
                var eval = CalculateEvalModel(shadowBoard, isWhite, false);
                var score = _neuralEvalService?.Predict(NormalizedEvalResultModel.From(eval)) ?? EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveSeq = moveSeq;
                    // we capture the features of the resulting board, this is the NN training input
                    bestFeatures = NormalizedEvalResultModel.From(eval);
                }
            }

            return new FinalEvalResult(bestMoveSeq, bestFeatures);
        }

        private CheapEvalResult[] GetCandidatesByCheapScore(
            IBoardModel board,
            MoveSequenceModel[] legalMovesSeq,
            bool isWhite,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights)
        {
            var candidates = new List<CheapEvalResult>();
            for (int index = 0; index < legalMovesSeq.Length; index++)
            {
                var shadowBoard = board.DeepClone();
                var moveSeq = legalMovesSeq[index];
                foreach (var move in moveSeq.Moves)
                {
                    BoardService.MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);
                }

                var isRace = RaceFeature.Eval(shadowBoard, isWhite);
                var eval = CalculateCheapEvalModel(shadowBoard, isWhite, isRace);
                // TODO: also calculate cheap score by neural net?
                var cheapScore = EvalScoreCalculator.CalculateCheapScore(eval, cheapContactWeights, raceWeights);
                var cheapEvalResult = new CheapEvalResult(cheapScore, index, isRace);
                candidates.Add(cheapEvalResult);
            }

            // we sort by cheap score descending and only fully evaluate the top N contact candidates.
            candidates.Sort((a, b) => b.CheapScore.CompareTo(a.CheapScore));
            return candidates.ToArray();
        }

        protected abstract EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace);

        protected abstract EvalResultModel CalculateCheapEvalModel(IBoardModel board, bool isWhite, bool isRace);
    }
}
