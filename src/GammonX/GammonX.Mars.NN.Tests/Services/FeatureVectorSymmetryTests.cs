using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Services;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Services;

public sealed class FeatureVectorSymmetryTests
{
    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public void EquivalentActivePlayerPerspectivesProduceTheSameFeatures(GameModus modus)
    {
        var boardService = BoardServiceFactory.Create(modus);
        var board = boardService.CreateBoard();
        AddRollHistory(board, 3);
        board.BearOffChecker(true, 3);
        board.BearOffChecker(false, 5);

        var invertedBoard = board.InvertBoard();
        AddRollHistory(invertedBoard, 3);

        var extractor = FeatureVectorExtractorFactory.Create(modus);
        var model = CreateAsymmetricModel();

        var activeBlackFeatures = extractor.Extract(model, board, false);
        var invertedActiveWhiteFeatures = extractor.Extract(model, invertedBoard, true);

        Assert.Equal(activeBlackFeatures.Length, extractor.FeatureCount);
        Assert.Equal(invertedActiveWhiteFeatures, activeBlackFeatures);
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public void TurnAndBearOffFeaturesUseTheCanonicalBoard(GameModus modus)
    {
        var boardService = BoardServiceFactory.Create(modus);
        var board = boardService.CreateBoard();
        AddRollHistory(board, 3);
        board.BearOffChecker(true, 3);
        board.BearOffChecker(false, 5);

        var extractor = FeatureVectorExtractorFactory.Create(modus);
        var features = extractor.Extract(CreateAsymmetricModel(), board, false);
        var metadataOffset = modus == GameModus.Plakoto ? 2 : 0;

        Assert.Equal(5f / 15f, features[21 + metadataOffset]);
        Assert.Equal(3f / 15f, features[22 + metadataOffset]);
        Assert.Equal(3f / 100f, features[23 + metadataOffset]);
        Assert.Equal(0f, features[20 + metadataOffset]);
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public void NeuralPredictionsMatchForEquivalentPerspectives(GameModus modus)
    {
        var boardService = BoardServiceFactory.Create(modus);
        var board = boardService.CreateBoard();
        AddRollHistory(board, 3);
        board.BearOffChecker(true, 3);
        board.BearOffChecker(false, 5);

        var invertedBoard = board.InvertBoard();
        AddRollHistory(invertedBoard, 3);

        var extractor = FeatureVectorExtractorFactory.Create(modus);
        var model = CreateAsymmetricModel();
        var activeBlackFeatures = extractor.Extract(model, board, false);
        var invertedActiveWhiteFeatures = extractor.Extract(model, invertedBoard, true);

        var net = NetModelFactory.Create(modus, CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A);
        net.Eval();

        using var activeBlackInput = tensor(activeBlackFeatures, device: CPU).unsqueeze(0);
        using var invertedActiveWhiteInput = tensor(invertedActiveWhiteFeatures, device: CPU).unsqueeze(0);
        using var activeBlackPrediction = net.Forward(activeBlackInput);
        using var invertedActiveWhitePrediction = net.Forward(invertedActiveWhiteInput);

        Assert.Equal(
            activeBlackPrediction.data<float>().ToArray(),
            invertedActiveWhitePrediction.data<float>().ToArray());
    }

    private static NormalizedEvalResultModel CreateAsymmetricModel()
    {
        return new NormalizedEvalResultModel
        {
            Race = false,
            PipDifference = 0.63,
            PipToBearOff = 0.21,
            PipToBearOffOpp = 0.79,
            MaxPrimeLengthPlayer = 0.13,
            MaxPrimeLengthOpp = 0.87,
            HomebarCountPlayer = 0.17,
            HomebarCountOpp = 0.83,
            BlotCount = 0.19,
            BlotCountOpp = 0.81,
            AnchorCountInFrontPlayer = 0.23,
            AnchorCountInFrontOpp = 0.77,
            AverageStackHeightPlayer = 0.29,
            AverageStackHeightOpp = 0.71,
            AverageDistanceToBearOffPlayer = 0.31,
            AverageDistanceToBearOffOpp = 0.69,
            AverageGapSizePlayer = 0.37,
            AverageGapSizeOpp = 0.63,
            CheckersInPrimeZonePlayer = 0.41,
            CheckersInPrimeZoneOpp = 0.59,
            PinCountPlayer = 0.43,
            PinCountOpp = 0.57,
            OppMotherPinned = 0.47,
            PlayerMotherPinned = 0.53,
            MotherDistancePlayer = 0.11,
            MotherDistanceOpp = 0.89,
            NumChFrontLastPin = 0.07,
            NumChFrontLastPinOpp = 0.93,
            BlotInStartRangeCount = 0.05,
            BlotInStartRangeCountOpp = 0.95,
        };
    }

    private static void AddRollHistory(IBoardModel board, int count)
    {
        var history = (IEditableBoardHistory)board.History;
        for (var i = 0; i < count; i++)
            history.Add(HistoryEventFactory.CreateRollEvent(i % 2 == 0, 1, 2));
    }
}