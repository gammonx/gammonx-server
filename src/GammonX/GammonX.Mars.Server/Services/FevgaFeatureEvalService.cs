using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.Server.Features;
using GammonX.Mars.Server.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Services
{
    // TODO implement

    // <inheritdoc />
    public sealed class FevgaFeatureEvalService : IFeatureEvalService
    {
        private readonly IBoardService _boardService = BoardServiceFactory.Create(GameModus.Fevga);

        private readonly RaceFeature _raceFeature = new RaceFeature();
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly MaxPrimeLengthFeature _maxPrimeLengthFeature = new MaxPrimeLengthFeature();
        private readonly HomebarCountFeature _homebarCountFeature = new HomebarCountFeature();
        // we see a hight blot count in fevga as a positive board control feature
        private readonly BlotCountFeature _blotCountFeature = new BlotCountFeature();
        private readonly PrimeProbabilityFeature _primeProbabilityFeature;

        public FevgaFeatureEvalService()
        {
            _primeProbabilityFeature = new PrimeProbabilityFeature(_boardService);
        }

        // <inheritdoc />
        public double EvalBoardState(
            EvalBoardRequestContract contract,
            ContactWeightModel cheapContactWeight, 
            ContactWeightModel contactWeights, 
            RaceWeightModel raceWeights)
        {
            var boardContract = contract.Board;
            var board = _boardService.CreateBoard(boardContract);
            var isWhite = contract.IsWhite;

            var isRace = _raceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);

            var score = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);
            return score;
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequence(
            EvalMoveRequestContract contract,
            ContactWeightModel cheapContactWeights, 
            ContactWeightModel contactWeights, 
            RaceWeightModel raceWeights)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            var isWhite = contract.IsWhite;

            var board = _boardService.CreateBoard(boardContract);
            var legalMovesSeq = _boardService.GetLegalMoveSequences(board, isWhite, rolls);

            if (legalMovesSeq.Length == 0)
                return new MoveSequenceModel();

            // we first compute cheap features to rank candidates, avoiding the expensive
            // e.g. ContactProbabilityFeature which internally explores all 21 dice combinations.
            var candidates = new (double cheapScore, int index, bool isRace)[legalMovesSeq.Length];
            for (int i = 0; i < legalMovesSeq.Length; i++)
            {
                var shadowBoard = board.DeepClone();
                var moveSeq = legalMovesSeq[i];
                foreach (var move in moveSeq.Moves)
                {
                    _boardService.MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);
                }

                var isRace = _raceFeature.Eval(shadowBoard, isWhite);
                candidates[i].isRace = isRace;
                candidates[i].index = i;
                var eval = CalculateCheapEvalModel(shadowBoard, isWhite, isRace);
                candidates[i].cheapScore = EvalScoreCalculator.CalculateCheapScore(eval, cheapContactWeights, raceWeights);
            }

            // we sort by cheap score descending and only fully evaluate the top N contact candidates.
            Array.Sort(candidates, (a, b) => b.cheapScore.CompareTo(a.cheapScore));

            const int defaultMaxFullEvalCandidates = 20;
            var identicalTopEvalCandidates = candidates.Count(c => c.cheapScore == candidates[0].cheapScore);

            var evalCount = Math.Min(Math.Max(defaultMaxFullEvalCandidates, identicalTopEvalCandidates), candidates.Length);

            double bestScore = double.MinValue;
            MoveSequenceModel bestMoveSeq = legalMovesSeq[candidates[0].index];

            for (int i = 0; i < evalCount; i++)
            {
                var idx = candidates[i].index;
                var moveSeq = legalMovesSeq[idx];

                if (candidates[i].isRace)
                {
                    // race score are already calculated
                    if (candidates[i].cheapScore > bestScore)
                    {
                        bestScore = candidates[i].cheapScore;
                        bestMoveSeq = moveSeq;
                    }
                    continue;
                }

                var shadowBoard = board.DeepClone();
                foreach (var move in moveSeq.Moves)
                {
                    _boardService.MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);
                }

                // we now calculate the more expensive contact features
                var eval = CalculateEvalModel(shadowBoard, isWhite, false);
                var score = EvalScoreCalculator.CalculateScore(eval, contactWeights, raceWeights);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoveSeq = moveSeq;
                }
            }

            return bestMoveSeq;
        }

        private EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace)
        {
            EvalResultModel eval;
            if (isRace)
            {
                // skip expensive probability features in race positions
                eval = new EvalResultModel()
                {
                    Race = true,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                };
            }
            else
            {
                var contactEvalResult = _primeProbabilityFeature.Eval(board, isWhite);

                eval = new EvalResultModel()
                {
                    Race = false,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                    MaxPrimeLengthPlayer = _maxPrimeLengthFeature.Eval(board, isWhite),
                    MaxPrimeLengthOpp = _maxPrimeLengthFeature.Eval(board, !isWhite),
                    HomebarCountPlayer = _homebarCountFeature.Eval(board, isWhite),
                    // we need to invert the count in order to get the normalized value correct
                    BlotCount = -_blotCountFeature.Eval(board, isWhite),
                    PrimeProbabilityPlayer = contactEvalResult.PrimeProbabilityPlayer,
                    PrimeProbabilityOpp = contactEvalResult.PrimeProbabilityOpp,
                };
            }

            return eval;
        }

        private EvalResultModel CalculateCheapEvalModel(IBoardModel board, bool isWhite, bool isRace)
        {
            EvalResultModel eval;
            if (isRace)
            {
                // skip expensive probability features in race positions
                eval = new EvalResultModel()
                {
                    Race = true,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                };
            }
            else
            {
                eval = new EvalResultModel()
                {
                    Race = false,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                    MaxPrimeLengthPlayer = _maxPrimeLengthFeature.Eval(board, isWhite),
                    MaxPrimeLengthOpp = _maxPrimeLengthFeature.Eval(board, !isWhite),
                    HomebarCountPlayer = _homebarCountFeature.Eval(board, isWhite),
                    // we need to invert the count in order to get the normalized value correct
                    BlotCount = -_blotCountFeature.Eval(board, isWhite),
                };
            }

            return eval;
        }
    }
}
