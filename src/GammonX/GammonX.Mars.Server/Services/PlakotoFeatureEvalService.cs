using GammonX.Models.Contracts;

using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Features;
using GammonX.Mars.Server.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Services
{
    // <inheritdoc />
    public sealed class PlakotoFeatureEvalService : IFeatureEvalService
    {
        private readonly IBoardService _boardService = BoardServiceFactory.Create(GameModus.Plakoto);

        private readonly RaceFeature _raceFeature = new RaceFeature();
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly NumbersOfCheckersInFronOfLastPinFeature _numChFronLastPinFeature = new NumbersOfCheckersInFronOfLastPinFeature();
        private readonly ContactProbabilityFeature _contactFeatures;
        private readonly PinEvalFeature _pinEvalFeature = new PinEvalFeature();

        public PlakotoFeatureEvalService()
        {
            _contactFeatures = new ContactProbabilityFeature(_boardService);
        }

        // <inheritdoc />
        public double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var boardContract = contract.Board;
            var board = _boardService.CreateBoard(boardContract);
            // bot always plays as black
            var isWhite = false;

            var isRace = _raceFeature.Eval(board, isWhite);
            var eval = CalculateEvalModel(board, isWhite, isRace);

            var score = CalculateEvalScore(eval, contactWeights, raceWeights);
            return score;
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequence(EvalMoveRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var rolls = contract.Rolls;
            var boardContract = contract.Board;
            // bot always plays as black
            var isWhite = false;

            var board = _boardService.CreateBoard(boardContract);
            var legalMovesSeq = _boardService.GetLegalMoveSequences(board, isWhite, rolls);

            var evals = new Dictionary<double, MoveSequenceModel>();

            foreach (var moveSeq in legalMovesSeq)
            {
                var shadowBoard = board.DeepClone();
                foreach (var move in moveSeq.Moves)
                {
                    _boardService.MoveCheckerTo(shadowBoard, move.From, move.To, isWhite);
                }

                var isRace = _raceFeature.Eval(shadowBoard, isWhite);

                var eval = CalculateEvalModel(shadowBoard, isWhite, isRace);

                var score = CalculateEvalScore(eval, contactWeights, raceWeights);
                evals.TryAdd(score, moveSeq);
            }

            if (evals.Count != 0)
            {
                var bestMove = evals.OrderByDescending(kvp => kvp.Key).FirstOrDefault();
                return bestMove.Value;
            }
            return new MoveSequenceModel();
        }

        private EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace)
        {
            EvalResultModel eval;
            if (isRace)
            {
                // race: only pip-based features matter, skip expensive probability features
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
                };
            }

            return eval;
        }

        private static double CalculateEvalScore(EvalResultModel eval, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            var normalizedResult = NormalizedEvalResultModel.From(eval);
            var score = 0.0;

            // we exclude contact based features in race positions, as they are not relevant and can be misleading
            if (eval.Race)
            {
                // good for the player
                score += normalizedResult.PipDifference * raceWeights.PipDifferenceWeight;
                score += normalizedResult.PipToBearOffOpp * raceWeights.PipToBearOffOppWeight;
                // bad for the player
                score -= normalizedResult.PipToBearOff * raceWeights.PipToBearOffWeight;
            }
            else
            {
                // good for the player
                score += normalizedResult.HitOpponentProbability1 * contactWeights.HitOpponentProbability1Weight;
                score += normalizedResult.HitOpponentProbability2 * contactWeights.HitOpponentProbability2Weight;
                score += normalizedResult.PipDifference * contactWeights.PipDifferenceWeight;
                score += normalizedResult.PipToBearOffOpp * contactWeights.PipToBearOffOppWeight;
                score += normalizedResult.NumChFrontLastPin * contactWeights.NumChFrontLastPinWeight;
                score += normalizedResult.EscapeProbability1 * contactWeights.EscapeProbability1Weight;
                score += normalizedResult.EscapeProbability2 * contactWeights.EscapeProbability2Weight;
                score += normalizedResult.PinCountOpp * contactWeights.PinCountOppWeight;
                score += normalizedResult.OppMotherPinned * contactWeights.OppMotherPinnedWeight;
                // bad for the player
                score -= normalizedResult.HitProbability1 * contactWeights.HitProbability1Weight;
                score -= normalizedResult.HitProbability2 * contactWeights.HitProbability2Weight;
                score -= normalizedResult.PipToBearOff * contactWeights.PipToBearOffWeight;
                score -= normalizedResult.NumChFrontLastPinOpp * contactWeights.NumChFrontLastPinOppWeight;
                score -= normalizedResult.EscapeProbability1Opp * contactWeights.EscapeProbability1OppWeight;
                score -= normalizedResult.EscapeProbability2Opp * contactWeights.EscapeProbability2OppWeight;
                score -= normalizedResult.PinCountPlayer * contactWeights.PinCountPlayerWeight;
                score -= normalizedResult.PlayerMotherPinned * contactWeights.PlayerMotherPinnedWeight;
            }

            return score;
        }
    }
}
