using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Mars.NN.Services
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
        private readonly PinEvalFeature _pinEvalFeature = new PinEvalFeature();
        private readonly MotherDistanceFeature _motherDistanceFeature = new MotherDistanceFeature();
        private readonly AverageStackHeightFeature _averageStackHeightFeature = new AverageStackHeightFeature();
        private readonly AverageDistanceToBearOffFeature _averageDistancePositionFeature = new AverageDistanceToBearOffFeature();

        protected override IBoardService BoardService { get; }

        public PlakotoFeatureEvalService(
            [FromKeyedServices(GameModus.Plakoto)] INeuralEvalService neuralEvalService) : base(neuralEvalService)
        {
            BoardService = BoardServiceFactory.Create(GameModus.Plakoto);
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
                //var (contactEvalPlayer, contactEvalOpp) = _contactFeatures.Eval(board, isWhite);
                var pinEval = _pinEvalFeature.Eval(board, isWhite);

                eval = new EvalResultModel()
                {
                    Race = false,
                    PipToBearOff = _pipsToBearOffFeature.Eval(board, isWhite),
                    PipToBearOffOpp = _pipsToBearOffFeature.Eval(board, !isWhite),
                    PipDifference = _pipDifferenceFeature.Eval(board, isWhite),
                    NumChFrontLastPin = _numChFronLastPinFeature.Eval(board, isWhite),
                    NumChFrontLastPinOpp = _numChFronLastPinFeature.Eval(board, !isWhite),
                    PinCountOpp = pinEval.PinnedOppCount,
                    PinCountPlayer = pinEval.PinnedPlayerCount,
                    OppMotherPinned = pinEval.OppMotherPinned,
                    PlayerMotherPinned = pinEval.PlayerMotherPinned,
                    AnchorCount = _anchorCountFeature.Eval(board, isWhite),
                    AnchorCountOpp = _anchorCountFeature.Eval(board, !isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    BlotCountOpp = _blotCountFeature.Eval(board, !isWhite),
                    BlotInStartRangeCount = _blotStartRangeCountFeature.Eval(board, isWhite),
                    BlotInStartRangeCountOpp = _blotStartRangeCountFeature.Eval(board, !isWhite),
                    MotherDistancePlayer = _motherDistanceFeature.Eval(board, isWhite),
                    MotherDistanceOpp = _motherDistanceFeature.Eval(board, !isWhite),
                    AverageStackHeightPlayer = _averageStackHeightFeature.Eval(board, isWhite),
                    AverageStackHeightOpp = _averageStackHeightFeature.Eval(board, !isWhite),
                    AverageDistanceToBearOffPlayer = _averageDistancePositionFeature.Eval(board, isWhite),
                    AverageDistanceToBearOffOpp = _averageDistancePositionFeature.Eval(board, !isWhite),
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
                    AnchorCountOpp = _anchorCountFeature.Eval(board, !isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    BlotCountOpp = _blotCountFeature.Eval(board, !isWhite),
                    BlotInStartRangeCount = _blotStartRangeCountFeature.Eval(board, isWhite),
                    BlotInStartRangeCountOpp = _blotStartRangeCountFeature.Eval(board, !isWhite),
                    AverageStackHeightPlayer = _averageStackHeightFeature.Eval(board, isWhite),
                    AverageStackHeightOpp = _averageStackHeightFeature.Eval(board, !isWhite),
                    AverageDistanceToBearOffPlayer = _averageDistancePositionFeature.Eval(board, isWhite),
                    AverageDistanceToBearOffOpp = _averageDistancePositionFeature.Eval(board, !isWhite),
                };
            }

            return eval;
        }
    }
}
