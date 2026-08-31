using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Nets;

public sealed class DefaultNetTests
{
    [Fact]
    public void MonotonicOutputModeProducesAValidFiveHeadHierarchy()
    {
        using var model = new DefaultNet(CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A);
        using var input = tensor(new float[3 * 216], [3, 216], device: CPU);
        using var noGradScope = no_grad();
        using var output = model.Forward(input);

        Assert.Equal([3L, 5L], output.shape);

        var values = output.data<float>().ToArray();
        for (var row = 0; row < 3; row++)
        {
            var predictions = values[(row * GameOutcomeConstraintValidator.FullHeadCount)..((row + 1) * GameOutcomeConstraintValidator.FullHeadCount)];
            var report = GameOutcomeConstraintValidator.Validate(predictions);
            Assert.True(report.IsValid, report.ToString());
        }
    }

    [Fact(Skip = "create for model requires real .dat file")]
    public void MissingMetadataCreatesLegacyOutputMode()
    {
        var modelPath = Path.Combine(Path.GetTempPath(), $"gammonx-{Guid.NewGuid():N}.dat");
        try
        {
            var model = Assert.IsType<DefaultNet>(NetModelFactory.CreateForModel(GameModus.Backgammon, modelPath, CPU));
            Assert.Equal(GameOutcomeOutputMode.LegacyIndependentSigmoid, model.OutputMode);
        }
        finally
        {
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    public void MetadataSelectsMonotonicOutputMode(GameModus gameModus)
    {
        // TODO: append other game modus
        // TODO: metadata issue > default == backgammon?
        var modelPath = Path.Combine("Data/NeuralNets", $"{gameModus}", "training_net.dat");
        try
        {
            NetModelMetadata.Write(modelPath, gameModus, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A);

            var model = Assert.IsType<DefaultNet>(NetModelFactory.CreateForModel(gameModus, modelPath, CPU));
            Assert.Equal(GameOutcomeOutputMode.MonotonicCumulative, model.OutputMode);
        }
        finally
        {
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }
}
