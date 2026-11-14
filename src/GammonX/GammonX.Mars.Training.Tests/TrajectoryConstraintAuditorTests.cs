using GammonX.Mars.NN.Nets;
using GammonX.Mars.Training.Sidecars;
using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class TrajectoryConstraintAuditorTests
{
    [Fact]
    public void SourceSidecarReportsRawFiveHeadViolations()
    {
        var trajectoryPath = CreateTemporaryPath("trajectory.csv");
        try
        {
            WriteTrajectory(
                trajectoryPath,
                [
                    [0.7f, 0.2f, 0.05f, 0.1f, 0.02f],
                    [0.4f, 0.6f, 0.8f, 0.9f, 0.95f]
                ]);

            var report = TrajectoryConstraintAuditor.Analyze(
                GameModus.Backgammon,
                trajectoryPath,
                null,
                null,
                CPU);

            Assert.NotNull(report.SourceModel);
            Assert.Equal(2, report.SourceModel.RowCount);
            Assert.Equal(1, report.SourceModel.InvalidRowCount);
            Assert.Equal(0.5, report.SourceModel.InvalidRate, 5);
            Assert.Null(report.CurrentModel);
        }
        finally
        {
            File.Delete(trajectoryPath);
        }
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public void CurrentMonotonicModelReportsNoRawViolations(GameModus modus)
    {
        var trajectoryPath = CreateTemporaryPath("trajectory.csv");
        var trainingPath = CreateTemporaryPath("training.csv");
        var modelPath = CreateTemporaryPath("model.dat");
        try
        {
            WriteTrajectory(trajectoryPath, [[0.7f, 0.2f, 0.05f, 0.1f, 0.02f]]);
            WriteTraining(trainingPath, modus);

            using (var model = NetModelFactory.CreateNew(modus, CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.B))
            {
                model.Save(modelPath);
            }

            var report = TrajectoryConstraintAuditor.Analyze(
                modus,
                trajectoryPath,
                trainingPath,
                modelPath,
                CPU,
                batchSize: 1);

            Assert.NotNull(report.SourceModel);
            Assert.NotNull(report.CurrentModel);
            Assert.Equal(1, report.CurrentModel.RowCount);
            Assert.Equal(0, report.CurrentModel.InvalidRowCount);
            Assert.Equal(0, report.CurrentModel.RuleViolationCount);
        }
        finally
        {
            File.Delete(trajectoryPath);
            File.Delete(trainingPath);
            File.Delete(modelPath);
            File.Delete(NetModelMetadata.GetPath(modelPath));
        }
    }

    [Theory]
    [InlineData(GameModus.Plakoto)]
    [InlineData(GameModus.Fevga)]
    public void GammonModusReportsRawConstraintViolations(GameModus modus)
    {
        var trajectoryPath = CreateTemporaryPath("trajectory.csv");
        try
        {
            WriteTrajectory(trajectoryPath, [[0.4f, 0.6f, 0f, 0.9f, 0f]]);

            var report = TrajectoryConstraintAuditor.Analyze(
                modus,
                trajectoryPath,
                null,
                null,
                CPU);

            Assert.NotNull(report.SourceModel);
            Assert.Equal(1, report.SourceModel.InvalidRowCount);
            Assert.Equal(0, report.SourceModel.WinBackgammonHierarchyViolationRows);
            Assert.Equal(0, report.SourceModel.LoseBackgammonHierarchyViolationRows);
            Assert.Null(report.CurrentModel);
        }
        finally
        {
            File.Delete(trajectoryPath);
        }
    }

    private static string CreateTemporaryPath(string suffix)
        => Path.Combine(Path.GetTempPath(), $"gammonx-{Guid.NewGuid():N}-{suffix}");

    private static void WriteTrajectory(string path, IReadOnlyList<float[]> rows)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss");
        for (var index = 0; index < rows.Count; index++)
        {
            writer.Write(Guid.NewGuid().ToString("D"));
            writer.Write($",{index},1,{(index == rows.Count - 1 ? 1 : 0)}");
            writer.Write(',');
            writer.WriteLine(string.Join(',', rows[index].Select(value => value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture))));
        }
    }

    private static void WriteTraining(string path, GameModus modus)
    {
        var count = modus == GameModus.Plakoto ? 266 : 216;
        var labelCount = WellKnownCsvHeaders.LabelCount(modus);
        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(',', Enumerable.Range(0, count).Select(index => $"f{index}")) + "," + WellKnownCsvHeaders.TrainingHeader(labelCount));
        writer.WriteLine(string.Join(',', Enumerable.Repeat("0", count)) + (labelCount == 3 ? ",0.5,0,0" : ",0.5,0,0,0,0"));
    }
}
