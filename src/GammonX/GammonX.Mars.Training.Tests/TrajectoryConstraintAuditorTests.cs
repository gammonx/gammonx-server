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

    [Fact]
    public void CurrentMonotonicModelReportsNoRawViolations()
    {
        var trajectoryPath = CreateTemporaryPath("trajectory.csv");
        var trainingPath = CreateTemporaryPath("training.csv");
        var modelPath = CreateTemporaryPath("model.dat");
        try
        {
            WriteTrajectory(trajectoryPath, [[0.7f, 0.2f, 0.05f, 0.1f, 0.02f]]);
            WriteTraining(trainingPath);
            using (var model = new DefaultNet(CPU, GameOutcomeOutputMode.MonotonicCumulative))
            {
                model.Save(modelPath);
            }
            NetModelMetadata.Write(modelPath, GameModus.Backgammon, GameOutcomeOutputMode.MonotonicCumulative);

            var report = TrajectoryConstraintAuditor.Analyze(
                GameModus.Backgammon,
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

    [Fact]
    public void SingleHeadModusDoesNotProduceFiveHeadMetrics()
    {
        var trajectoryPath = CreateTemporaryPath("trajectory.csv");
        try
        {
            WriteTrajectory(trajectoryPath, [[0.4f, 0.6f, 0.8f, 0.9f, 0.95f]]);

            var report = TrajectoryConstraintAuditor.Analyze(
                GameModus.Plakoto,
                trajectoryPath,
                null,
                null,
                CPU);

            Assert.Null(report.SourceModel);
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

    private static void WriteTraining(string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine(string.Join(',', Enumerable.Range(0, 216).Select(index => $"f{index}")) + ",pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss");
        writer.WriteLine(string.Join(',', Enumerable.Repeat("0", 216)) + ",0.5,0,0,0,0");
    }
}
