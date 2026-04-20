using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.Server.Features;
using GammonX.Mars.Server.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.Server.Services
{
    // <inheritdoc />
    public sealed class PlakotoFeatureEvalService : BaseFeatureEvalServiceImpl
    {
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly BlotCountFeature _blotCountFeature = new BlotCountFeature();
        private readonly BlotInStartRangeCountFeature _blotStartRangeCountFeature = new BlotInStartRangeCountFeature();
        private readonly AnchorCountFeature _anchorCountFeature = new AnchorCountFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly NumbersOfCheckersInFronOfLastPinFeature _numChFronLastPinFeature = new NumbersOfCheckersInFronOfLastPinFeature();
        private readonly ContactProbabilityFeature _contactFeatures;
        private readonly PinEvalFeature _pinEvalFeature = new PinEvalFeature();

        protected override IBoardService BoardService { get; }

        public PlakotoFeatureEvalService()
        {
            BoardService = BoardServiceFactory.Create(GameModus.Plakoto);
            _contactFeatures = new ContactProbabilityFeature(BoardService);
        }
        protected override EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace)
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

        protected override EvalResultModel CalculateCheapEvalModel(IBoardModel board, bool isWhite, bool isRace)
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
