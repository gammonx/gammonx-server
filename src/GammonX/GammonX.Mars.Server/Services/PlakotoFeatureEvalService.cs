using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.Server.Features;
using GammonX.Mars.Server.Models;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Services
{
    // <inheritdoc />
    public sealed class PlakotoFeatureEvalService : IFeatureEvalService
    {
        private readonly IBoardService _boardService = BoardServiceFactory.Create(GameModus.Plakoto);

        private readonly RaceFeature _raceFeature = new RaceFeature();
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly BlotCountFeature _blotCountFeature = new BlotCountFeature();
        private readonly BlotInStartRangeCountFeature _blotStartRangeCountFeature = new BlotInStartRangeCountFeature();
        private readonly AnchorCountFeature _anchorCountFeature = new AnchorCountFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly NumbersOfCheckersInFronOfLastPinFeature _numChFronLastPinFeature = new NumbersOfCheckersInFronOfLastPinFeature();
        private readonly ContactProbabilityFeature _contactFeatures;
        private readonly PinEvalFeature _pinEvalFeature = new PinEvalFeature();

        public PlakotoFeatureEvalService()
        {
            _contactFeatures = new ContactProbabilityFeature(_boardService);
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
                var contactEvalPlayer = _contactFeatures.Eval(board, isWhite);
                var contactEvalOpp = _contactFeatures.Eval(board, !isWhite);
                var pinEval = _pinEvalFeature.Eval(board, isWhite);

                eval = new EvalResultModel()
                {
                    Race = false,
                    HitProbability1 = contactEvalPlayer.HitProbability1,
                    HitProbability2 = contactEvalPlayer.HitProbability2,
                    HitOpponentProbability1 = contactEvalOpp.HitProbability1,
                    HitOpponentProbability2 = contactEvalOpp.HitProbability2,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                    NumChFrontLastPin = _numChFronLastPinFeature.Eval(board, isWhite),
                    NumChFrontLastPinOpp = _numChFronLastPinFeature.Eval(board, !isWhite),
                    EscapeProbability1 = contactEvalPlayer.EscapeProbability1,
                    EscapeProbability2 = contactEvalPlayer.EscapeProbability2,
                    EscapeProbability1Opp = contactEvalOpp.EscapeProbability1,
                    EscapeProbability2Opp = contactEvalOpp.EscapeProbability2,
                    PinCountOpp = pinEval.PinnedOppCount,
                    PinCountPlayer = pinEval.PinnedPlayerCount,
                    OppMotherPinned = pinEval.OppMotherPinned,
                    PlayerMotherPinned = pinEval.PlayerMotherPinned,
                    AnchorCount = _anchorCountFeature.Eval(board, isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    BlotInStartRangeCount = _blotStartRangeCountFeature.Eval(board, isWhite),
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
                var pinEval = _pinEvalFeature.Eval(board, isWhite);

                eval = new EvalResultModel()
                {
                    Race = false,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                    PinCountOpp = pinEval.PinnedOppCount,
                    PinCountPlayer = pinEval.PinnedPlayerCount,
                    AnchorCount = _anchorCountFeature.Eval(board, isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    BlotInStartRangeCount = _blotStartRangeCountFeature.Eval(board, isWhite),
                };
            }

            return eval;
        }
    }
}
