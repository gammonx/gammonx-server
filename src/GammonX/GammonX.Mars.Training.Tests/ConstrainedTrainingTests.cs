using GammonX.Mars.NN.Nets;

using GammonX.Mars.Training.Train;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class ConstrainedTrainingTests
{
    [Theory]
    [InlineData(GameModus.Backgammon, false)]
    [InlineData(GameModus.Backgammon, true)]
    [InlineData(GameModus.Fevga, false)]
    [InlineData(GameModus.Fevga, true)]
    [InlineData(GameModus.Plakoto, false)]
    [InlineData(GameModus.Plakoto, true)]
    public void ConstrainedTrainingWritesMetadataAndValidPredictions(GameModus modus, bool resume)
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
            WriteDataset(trainingPath, modus);
            WriteDataset(validationPath, modus);

            using var trainingModel = NetModelFactory.CreateNew(modus, CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B);
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

            var metadata = NetModelMetadata.ReadOrLegacy(modelPath, modus);
            Assert.Equal(GameOutcomeOutputMode.MonotonicCumulative, metadata.OutputMode);
            Assert.Equal(NetArchitecture.B, metadata.Architecture);
            Assert.Equal(Path.GetFileName(modelPath), metadata.Name);
            Assert.Equal(training, metadata.Training);

            using var model = NetModelFactory.CreateForModel(modus, modelPath, CPU);
            model.Eval();
            var featureCount = modus == GameModus.Plakoto ? 266 : 216;
            using var input = tensor(new float[2 * featureCount], [2, featureCount], device: CPU);
            using var noGradScope = no_grad();
            using var predictions = model.Forward(input);
            var metrics = new ConstraintMetricsAccumulator();
            metrics.Add(predictions);

            Assert.Equal(2, metrics.Complete().RowCount);
            Assert.Equal(0, metrics.Complete().InvalidRowCount);

            if (resume)
            {
                using var parent = NetModelFactory.CreateForModel(modus, inputModelPath, CPU);
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

    [Theory]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public void LegacySingleHeadTrainingIsRejectedBeforeReadingDatasets(GameModus modus)
    {
        using var model = NetModelFactory.CreateNew(modus, CPU, GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A);
        model.ConfigureTraining(new NetTrainingMetadata("missing-train.csv", "missing-val.csv", "unused.dat"));
        var error = Assert.Throws<InvalidOperationException>(() => NetTrainer.Train(model));
        Assert.Contains("inference only", error.Message);
    }

    private static void WriteDataset(string path, GameModus modus)
    {
        var labels = new[]
        {
            new[] { 0.7f, 0.2f, 0.05f, 0.1f, 0.02f },
            new[] { 0.4f, 0.1f, 0.02f, 0.3f, 0.1f },
            new[] { 0.2f, 0.05f, 0.01f, 0.5f, 0.1f },
            new[] { 0.9f, 0.4f, 0.2f, 0.05f, 0.01f }
        };

        var featureCount = modus == GameModus.Plakoto ? 266 : 216;
        var labelCount = WellKnownCsvHeaders.LabelCount(modus);
        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(',', Enumerable.Range(0, featureCount).Select(index => $"f{index}")) + "," + WellKnownCsvHeaders.TrainingHeader(labelCount));
        foreach (var row in labels)
        {
            writer.WriteLine(
                string.Join(',', Enumerable.Repeat("0", featureCount))
                + ","
                + string.Join(',', (labelCount == 3 ? new[] { row[0], row[1], row[3] } : row).Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))));
        }
    }
}
