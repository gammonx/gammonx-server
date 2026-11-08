using GammonX.Mars.NN.Nets;

using GammonX.Mars.Training.Train;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class ConstrainedTrainingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstrainedTrainingWritesMetadataAndValidPredictions(bool resume)
    {
        var stem = $"gammonx-{Guid.NewGuid():N}";
        var trainingPath = Path.Combine(Path.GetTempPath(), stem + "-train.csv");
        var validationPath = Path.Combine(Path.GetTempPath(), stem + "-val.csv");
        var modelPath = Path.Combine(Path.GetTempPath(), stem + ".dat");
        var inputModelPath = Path.Combine(Path.GetTempPath(), stem + "-parent.dat");
        var trainingBinaryPath = Path.Combine(Environment.CurrentDirectory, stem + "-train.bin");
        var validationBinaryPath = Path.Combine(Environment.CurrentDirectory, stem + "-val.bin");

        try
        {
            WriteDataset(trainingPath);
            WriteDataset(validationPath);

            using var trainingModel = NetModelFactory.CreateNew(GameModus.Backgammon, CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B);
            if (resume)
            {
                trainingModel.Save(inputModelPath);
                trainingModel.Load(inputModelPath);
            }

            var training = new NetTrainingMetadata(
                trainingPath,
                validationPath,
                modelPath,
                InputModelPath: resume ? inputModelPath : null,
                Epochs: 1,
                BatchSize: 2,
                EarlyStoppingPatience: 1,
                MinimumDelta: resume ? float.MaxValue : 1e-5f);
            trainingModel.ConfigureTraining(training);
            NetTrainer.Train(trainingModel);

            Assert.True(File.Exists(modelPath));
            Assert.True(File.Exists(NetModelMetadata.GetPath(modelPath)));

            var metadata = NetModelMetadata.ReadOrLegacy(modelPath, GameModus.Backgammon);
            Assert.Equal(GameOutcomeOutputMode.MonotonicCumulative, metadata.OutputMode);
            Assert.Equal(NetArchitecture.B, metadata.Architecture);
            Assert.Equal(Path.GetFileName(modelPath), metadata.Name);
            Assert.Equal(training, metadata.Training);

            using var model = NetModelFactory.CreateForModel(GameModus.Backgammon, modelPath, CPU);
            model.Eval();
            using var input = tensor(new float[2 * 216], [2, 216], device: CPU);
            using var noGradScope = no_grad();
            using var predictions = model.Forward(input);
            var metrics = new ConstraintMetricsAccumulator();
            metrics.Add(predictions);

            Assert.Equal(2, metrics.Complete().RowCount);
            Assert.Equal(0, metrics.Complete().InvalidRowCount);

            if (resume)
            {
                using var parent = NetModelFactory.CreateForModel(GameModus.Backgammon, inputModelPath, CPU);
                parent.Eval();
                using var parentPredictions = parent.Forward(input);
                Assert.Equal(parentPredictions.data<float>().ToArray(), predictions.data<float>().ToArray());
            }
        }
        finally
        {
            File.Delete(trainingPath);
            File.Delete(validationPath);
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
            File.Delete(inputModelPath);
            File.Delete(NetModelMetadata.GetPath(inputModelPath));
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
        writer.WriteLine(string.Join(',', Enumerable.Range(0, 216).Select(index => $"f{index}")) + "," + WellKnownCsvHeaders.TrainingDataHeader);
        foreach (var row in labels)
        {
            writer.WriteLine(
                string.Join(',', Enumerable.Repeat("0", 216))
                + ","
                + string.Join(',', row.Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))));
        }
    }
}
