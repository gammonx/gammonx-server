using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Mars.NN.Services
{
    // <inheritdoc />
    public sealed class FevgaFeatureEvalService : BaseFeatureEvalServiceImpl
    {
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly MaxPrimeLengthFeature _maxPrimeLengthFeature = new MaxPrimeLengthFeature();
        private readonly HomebarCountFeature _homebarCountFeature = new HomebarCountFeature();
        // we see a high blot count in fevga as a positive board control feature
        private readonly BlotCountFeature _blotCountFeature = new BlotCountFeature();
        private readonly PrimeProbabilityFeature _primeProbabilityFeature;

        protected override IBoardService BoardService { get; }


        public FevgaFeatureEvalService(
            [FromKeyedServices(GameModus.Fevga)] INeuralEvalService neuralEvalService) : base(neuralEvalService)
        {
            BoardService = BoardServiceFactory.Create(GameModus.Fevga);
            _primeProbabilityFeature = new PrimeProbabilityFeature(BoardService);
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
