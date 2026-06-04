using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Mars.NN.Services
{
    /// <summary>
    /// Provides capabilities to evaluate board states and move sequences for <see cref="GameModus.Backgammon"/>, <see cref="GameModus.Tavla"/>
    /// and <see cref="GameModus.Portes"/>.
    /// </summary>
    /// <seealso cref="BaseFeatureEvalServiceImpl"/>
    public sealed class DefaultFeatureEvalService : BaseFeatureEvalServiceImpl
    {
        private readonly PipsToBearOffFeature _pipsToBearOffFeature = new PipsToBearOffFeature();
        private readonly PipDifferenceFeature _pipDifferenceFeature = new PipDifferenceFeature();
        private readonly MaxPrimeLengthFeature _maxPrimeLengthFeature = new MaxPrimeLengthFeature();
        private readonly HomebarCountFeature _homebarCountFeature = new HomebarCountFeature();
        private readonly BlotCountFeature _blotCountFeature = new BlotCountFeature();
        private readonly AnchorCountInFrontFeature _anchorCountInFrontFeature = new AnchorCountInFrontFeature();
        private readonly AverageStackHeightFeature _averageStackHeightFeature = new AverageStackHeightFeature();
        private readonly AverageDistanceToBearOffFeature _averageDistancePositionFeature = new AverageDistanceToBearOffFeature();
        private readonly AverageGapSizeFeature _averageGapSizeFeature = new AverageGapSizeFeature();
        private readonly CheckersInPrimeZoneFeature _checkersInPrimeZoneFeature = new CheckersInPrimeZoneFeature();

        // <inheritdoc />
        protected override IBoardService BoardService { get; }

        public DefaultFeatureEvalService(IServiceProvider services, GameModus modus) : base(services.GetKeyedService<INeuralEvalService>(modus)!)
        {
            BoardService = BoardServiceFactory.Create(modus);
        }

        public DefaultFeatureEvalService(INeuralEvalService neuralService, GameModus modus) : base(neuralService)
        {
            BoardService = BoardServiceFactory.Create(modus);
        }

        // <inheritdoc />
        protected override EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite, bool isRace)
        {
            EvalResultModel eval;
            if (isRace)
            {
                // skip expensive features in race positions
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
                    HomebarCountOpp = _homebarCountFeature.Eval(board, !isWhite),
                    BlotCountOpp = _blotCountFeature.Eval(board, !isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    AnchorCountInFrontPlayer = _anchorCountInFrontFeature.Eval(board, isWhite),
                    AnchorCountInFrontOpp = _anchorCountInFrontFeature.Eval(board, !isWhite),
                    AverageStackHeightPlayer = _averageStackHeightFeature.Eval(board, isWhite),
                    AverageStackHeightOpp = _averageStackHeightFeature.Eval(board, !isWhite),
                    AverageDistanceToBearOffPlayer = _averageDistancePositionFeature.Eval(board, isWhite),
                    AverageDistanceToBearOffOpp = _averageDistancePositionFeature.Eval(board, !isWhite),
                    AverageGapSizePlayer = _averageGapSizeFeature.Eval(board, isWhite),
                    AverageGapSizeOpp = _averageGapSizeFeature.Eval(board, !isWhite),
                    CheckersInPrimeZonePlayer = _checkersInPrimeZoneFeature.Eval(board, isWhite),
                    CheckersInPrimeZoneOpp = _checkersInPrimeZoneFeature.Eval(board, !isWhite),
                };
            }

            return eval;
        }

        // <inheritdoc />
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
                    HomebarCountOpp = _homebarCountFeature.Eval(board, !isWhite),
                    BlotCountOpp = _blotCountFeature.Eval(board, !isWhite),
                    BlotCount = _blotCountFeature.Eval(board, isWhite),
                    AnchorCountInFrontPlayer = _anchorCountInFrontFeature.Eval(board, isWhite),
                    AnchorCountInFrontOpp = _anchorCountInFrontFeature.Eval(board, !isWhite),
                    AverageStackHeightPlayer = _averageStackHeightFeature.Eval(board, isWhite),
                    AverageStackHeightOpp = _averageStackHeightFeature.Eval(board, !isWhite),
                    AverageDistanceToBearOffPlayer = _averageDistancePositionFeature.Eval(board, isWhite),
                    AverageDistanceToBearOffOpp = _averageDistancePositionFeature.Eval(board, !isWhite),
                    AverageGapSizePlayer = _averageGapSizeFeature.Eval(board, isWhite),
                    AverageGapSizeOpp = _averageGapSizeFeature.Eval(board, !isWhite),
                    CheckersInPrimeZonePlayer = _checkersInPrimeZoneFeature.Eval(board, isWhite),
                    CheckersInPrimeZoneOpp = _checkersInPrimeZoneFeature.Eval(board, !isWhite),
                };
            }

            return eval;
        }
    }
}