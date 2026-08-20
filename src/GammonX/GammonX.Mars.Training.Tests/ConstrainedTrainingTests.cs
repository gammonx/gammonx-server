using GammonX.Mars.NN.Nets;

using GammonX.Mars.Training.Train;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class ConstrainedTrainingTests
{
    [Fact]
    public void ConstrainedTrainingWritesMetadataAndValidPredictions()
    {
        var stem = $"gammonx-{Guid.NewGuid():N}";
        var trainingPath = Path.Combine(Path.GetTempPath(), stem + "-train.csv");
        var validationPath = Path.Combine(Path.GetTempPath(), stem + "-val.csv");
        var modelPath = Path.Combine(Path.GetTempPath(), stem + ".dat");
        var trainingBinaryPath = Path.Combine(Environment.CurrentDirectory, stem + "-train.bin");
        var validationBinaryPath = Path.Combine(Environment.CurrentDirectory, stem + "-val.bin");

        try
        {
            WriteDataset(trainingPath);
            WriteDataset(validationPath);

            NetTrainer.Train(
                GameModus.Backgammon,
                trainingPath,
                validationPath,
                modelPath,
                epochs: 1,
                batchSize: 2,
                producerCount: 1,
                queueCapacity: 2,
                earlyStoppingPatience: 1,
                useConstrainedOutputs: true);

            Assert.True(File.Exists(modelPath));
            Assert.True(File.Exists(NetModelMetadata.GetPath(modelPath)));

            var metadata = NetModelMetadata.ReadOrLegacy(modelPath, GameModus.Backgammon);
            Assert.Equal(GameOutcomeOutputMode.MonotonicCumulative, metadata.OutputMode);

            var model = NetModelFactory.CreateForModel(GameModus.Backgammon, modelPath, CPU);
            model.Load(modelPath);
            model.Eval();
            using var input = tensor(new float[2 * 216], [2, 216], device: CPU);
            using var noGradScope = no_grad();
            using var predictions = model.Forward(input);
            var metrics = new ConstraintMetricsAccumulator();
            metrics.Add(predictions);

            Assert.Equal(2, metrics.Complete().RowCount);
            Assert.Equal(0, metrics.Complete().InvalidRowCount);
        }
        finally
        {
            File.Delete(trainingPath);
            File.Delete(validationPath);
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
            File.Delete(trainingBinaryPath);
            File.Delete(validationBinaryPath);
        }
    }

    private static void WriteDataset(string path)
    {
        var labels = new[]
        {
            new[] { 0.7f, 0.2f, 0.05f, 0.1f, 0.02f },
            new[] { 0.4f, 0.1f, 0.02f, 0.3f, 0.1f },
            new[] { 0.2f, 0.05f, 0.01f, 0.5f, 0.1f },
            new[] { 0.9f, 0.4f, 0.2f, 0.05f, 0.01f }
        };

        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(',', Enumerable.Range(0, 216).Select(index => $"f{index}")) + ",pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss");
        foreach (var row in labels)
        {
            writer.WriteLine(
                string.Join(',', Enumerable.Repeat("0", 216))
                + ","
                + string.Join(',', row.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))));
        }
    }
}
