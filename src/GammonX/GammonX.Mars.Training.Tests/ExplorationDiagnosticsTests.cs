using GammonX.Mars.Training.Generator;
using GammonX.Mars.Training.Sidecars;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class ExplorationDiagnosticsTests
{
    [Fact]
    public void SidecarRoundTripPreservesDecisionFields()
    {
        var path = CreateTempPath();
        var gameId = Guid.NewGuid();
        var decisions = new[]
        {
            new ExplorationDecision(
                gameId,
                GameModus.Backgammon,
                1,
                true,
                false,
                4,
                1.25d,
                1.0d,
                0.25d,
                ExplorationChoice.Ranked,
                2,
                0.75d),
            new ExplorationDecision(
                gameId,
                GameModus.Backgammon,
                2,
                true,
                false,
                1,
                0.5d,
                null,
                null,
                ExplorationChoice.Greedy,
                0,
                0.5d)
        };

        try
        {
            ExplorationCsvWriter.WriteDecisions(path, decisions);
            var read = ExplorationCsvReader.ReadDecisions(path).ToArray();

            Assert.Equal(decisions, read);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void AnalyzerReportsQuantilesAndBreakdownsWithoutMissingGaps()
    {
        var decisions = Enumerable.Range(0, 5)
            .Select(index => new ExplorationDecision(
                Guid.NewGuid(),
                GameModus.Backgammon,
                index + 1,
                index < 2,
                index % 2 == 0,
                index + 1,
                index + 1d,
                index + 0.5d,
                index / 10d,
                index % 2 == 0 ? ExplorationChoice.Ranked : ExplorationChoice.Greedy,
                index % 2 == 0 ? index : 0,
                index + 0.25d))
            .ToArray();

        var report = ExplorationBroker.Analyze(decisions, reservoirCapacity: 5);

        Assert.Equal(5, report.Overall.DecisionCount);
        Assert.Equal(5, report.Overall.GapCount);
        Assert.Equal(0, report.Overall.NoGapCount);
        Assert.Equal(0.2d, report.Overall.P50Gap!.Value, 5);
        Assert.Equal(0.4d, report.Overall.MaxGap!.Value, 5);
        Assert.Equal(5, report.Overall.ChoiceCounts.Values.Sum());
        Assert.NotEmpty(report.Groups);
    }

    [Fact]
    public void AnalyzerCountsSingleCandidateTurnsWithoutAGap()
    {
        var decision = new ExplorationDecision(
            Guid.NewGuid(),
            GameModus.Tavla,
            1,
            true,
            false,
            1,
            0.5d,
            null,
            null,
            ExplorationChoice.Greedy,
            0,
            0.5d);

        var report = ExplorationBroker.Analyze([decision]);

        Assert.Equal(1, report.Overall.DecisionCount);
        Assert.Equal(0, report.Overall.GapCount);
        Assert.Equal(1, report.Overall.NoGapCount);
        Assert.Null(report.Overall.P50Gap);
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(3, "2-3")]
    [InlineData(7, "4-7")]
    [InlineData(15, "8-15")]
    [InlineData(16, "16+")]
    public void CandidateCountUsesStableBuckets(int candidateCount, string expectedBucket)
    {
        Assert.Equal(expectedBucket, ExplorationBroker.GetCandidateBucket(candidateCount));
    }

    private static string CreateTempPath()
        => Path.Combine(Path.GetTempPath(), $"gammonx-exploration-{Guid.NewGuid():N}.csv");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
