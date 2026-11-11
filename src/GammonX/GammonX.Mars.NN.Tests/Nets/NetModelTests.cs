using System.Text.Json;

using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Models;
using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Nets;

public sealed class NetModelTests
{
    [Theory]
    [InlineData(GameModus.Backgammon, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A)]
    [InlineData(GameModus.Backgammon, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.B)]
    [InlineData(GameModus.Tavla, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B)]
    [InlineData(GameModus.Portes, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A)]
    [InlineData(GameModus.Plakoto, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A)]
    [InlineData(GameModus.Fevga, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A)]
    [InlineData(GameModus.Fevga, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B)]
    [InlineData(GameModus.Plakoto, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B)]
    [InlineData(GameModus.Fevga, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.B)]
    [InlineData(GameModus.Plakoto, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.B)]
    public void SaveAndLoadPreserveMetadataAndPredictions(GameModus modus, GameOutcomeOutputMode outputMode, NetArchitecture architecture)
    {
        var modelPath = Path.Combine(Path.GetTempPath(), $"gammonx-{Guid.NewGuid():N}.dat");
        var renamedPath = Path.ChangeExtension(modelPath, ".renamed.dat");
        try
        {
            using var model = NetModelFactory.CreateNew(modus, CPU, outputMode, architecture);
            var training = new NetTrainingMetadata("train.csv", "val.csv", modelPath, BatchSize: 32, ShuffleLabels: true);
            model.ConfigureTraining(training);
            model.Eval();
            model.Save(modelPath);

            Assert.Equal(Path.GetFileName(modelPath), model.Metadata.Name);
            Assert.Equal(model.Metadata, NetModelMetadata.ReadOrLegacy(modelPath, modus));

            using var loaded = NetModelFactory.CreateForModel(modus, modelPath, CPU);
            using var weights = File.OpenRead(modelPath);
            using var metadata = File.OpenRead(NetModelMetadata.GetPath(modelPath));
            using var streamed = NetModelFactory.CreateForModel(modus, weights, metadata, CPU, "embedded_net.dat");
            Assert.Equal(model.Metadata, loaded.Metadata);
            Assert.Equal(model.Metadata with { Name = "embedded_net.dat" }, streamed.Metadata);

            loaded.Eval();
            streamed.Eval();
            var featureCount = modus == GameModus.Plakoto ? 266 : 216;
            using var input = tensor(new float[2 * featureCount], [2, featureCount], device: CPU);
            using var noGradScope = no_grad();
            using var expected = model.Forward(input);
            using var actual = loaded.Forward(input);
            using var streamedPrediction = streamed.Forward(input);
            Assert.Equal(expected.data<float>().ToArray(), actual.data<float>().ToArray());
            Assert.Equal(expected.data<float>().ToArray(), streamedPrediction.data<float>().ToArray());

            loaded.Save(renamedPath);
            Assert.Equal(Path.GetFileName(renamedPath), loaded.Metadata.Name);
            Assert.Equal(loaded.Metadata, NetModelMetadata.ReadOrLegacy(renamedPath, modus));
            using var savedMetadata = JsonDocument.Parse(File.ReadAllText(NetModelMetadata.GetPath(renamedPath)));
            Assert.Equal(Path.GetFileName(renamedPath), savedMetadata.RootElement.GetProperty(nameof(NetModelMetadata.Name)).GetString());
        }
        finally
        {
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
            File.Delete(renamedPath);
            File.Delete(NetModelMetadata.GetPath(renamedPath));
        }
    }

    [Theory]
    [InlineData(GameModus.Backgammon, false, NetArchitecture.A)]
    [InlineData(GameModus.Backgammon, true, NetArchitecture.A)]
    [InlineData(GameModus.Plakoto, false, NetArchitecture.A)]
    [InlineData(GameModus.Fevga, false, NetArchitecture.A)]
    [InlineData(GameModus.Plakoto, true, NetArchitecture.B)]
    [InlineData(GameModus.Fevga, true, NetArchitecture.B)]
    public void SavingLegacyModelsUpgradesMetadataWithoutInventingTrainingSettings(GameModus modus, bool hasMetadata, NetArchitecture recordedArchitecture)
    {
        var modelPath = Path.Combine(Path.GetTempPath(), $"gammonx-{Guid.NewGuid():N}.dat");
        try
        {
            using (var original = NetModelFactory.CreateNew(modus, CPU, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A))
                ((NetModel)original).save(modelPath);

            if (hasMetadata)
            {
                File.WriteAllText(NetModelMetadata.GetPath(modelPath), $$"""
                    { "FormatVersion": 1, "Modus": "{{modus}}", "OutputMode": "LegacyIndependentSigmoid", "Architecture": "{{recordedArchitecture}}" }
                    """);
            }

            using var loaded = NetModelFactory.CreateForModel(modus, modelPath, CPU);
            Assert.Equal(hasMetadata ? 1 : 0, loaded.Metadata.FormatVersion);
            Assert.Equal(NetArchitecture.A, loaded.Metadata.Architecture);
            Assert.Null(loaded.Metadata.Training);
            Assert.Equal(Path.GetFileName(modelPath), loaded.Metadata.Name);

            using (var weights = File.OpenRead(modelPath))
            using (var metadata = hasMetadata ? File.OpenRead(NetModelMetadata.GetPath(modelPath)) : null)
            using (var streamed = NetModelFactory.CreateForModel(modus, weights, metadata, CPU, "legacy.dat"))
            {
                Assert.Equal(loaded.Metadata with { Name = "legacy.dat" }, streamed.Metadata);
            }

            loaded.Save(modelPath);
            var saved = NetModelMetadata.ReadOrLegacy(modelPath, modus);
            Assert.Equal(NetModelMetadata.CurrentFormatVersion, saved.FormatVersion);
            Assert.Equal(GameOutcomeOutputMode.LegacyIndependentSigmoid, saved.OutputMode);
            Assert.Null(saved.Training);
        }
        finally
        {
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }

    [Theory]
    [InlineData(GameModus.Fevga, 216, 384, 192, 96)]
    [InlineData(GameModus.Plakoto, 266, 440, 220, 110)]
    public void GammonArchitectureHasRequestedLayersAndConstrainedThreeHeadOutputs(
        GameModus modus, int inputs, int hidden1, int hidden2, int hidden3)
    {
        using var model = NetModelFactory.CreateNew(modus, CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B);
        long[][] shapes = [[hidden1, inputs], [hidden1], [hidden2, hidden1], [hidden2], [hidden3, hidden2], [hidden3], [3, hidden3], [3]];
        Assert.Equal(shapes, model.GetParameters().Select(p => p.shape));
        using var noGradScope = no_grad();
        foreach (var batchSize in new[] { 1, 7 })
        {
            using var input = randn([batchSize, inputs], device: CPU);
            using var output = model.Forward(input);
            Assert.Equal([batchSize, 3L], output.shape);
            var values = output.data<float>().ToArray();
            for (var row = 0; row < batchSize; row++)
            {
                var canonical = GameOutcomeModel.ToCanonical(values.AsSpan(row * 3, 3));
                Assert.True(GameOutcomeConstraintValidator.Validate(canonical).IsValid);
                Assert.Equal(0f, canonical[2]);
                Assert.Equal(0f, canonical[4]);
            }
        }
        Assert.Throws<InvalidDataException>(() => NetModelFactory.CreateNew(modus, CPU,
            GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A));
    }

    [Fact]
    public void LoadRejectsIncompatibleMetadataBeforeChangingTheNetwork()
    {
        var modelPath = Path.Combine(Path.GetTempPath(), $"gammonx-{Guid.NewGuid():N}.dat");
        try
        {
            using var original = new DefaultNet(CPU, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A);
            original.Save(modelPath);
            using var incompatible = new DefaultNet(CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A);

            Assert.Throws<InvalidDataException>(() => incompatible.Load(modelPath));
            Assert.Equal(GameOutcomeOutputMode.MonotonicCumulative, incompatible.Metadata.OutputMode);
        }
        finally
        {
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }
}
