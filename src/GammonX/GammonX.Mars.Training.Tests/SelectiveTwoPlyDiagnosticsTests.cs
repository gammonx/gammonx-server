using GammonX.Mars.Training.Generator;
using GammonX.Mars.Training.Sidecars;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class SelectiveTwoPlyDiagnosticsTests
{
    [Fact]
    public void SidecarRoundTripPreservesDecisions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gammonx-search-{Guid.NewGuid():N}.csv");
        var decisions = CreateDecisions();

        try
        {
            SelectiveTwoPlySearchCsvWriter.WriteDecisions(path, decisions);
            var read = SelectiveTwoPlySearchCsvReader.ReadDecisions(path).ToArray();

            Assert.Equal(decisions, read);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ReaderAcceptsLegacySidecarWithoutAuditRankFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gammonx-search-legacy-{Guid.NewGuid():N}.csv");
        var gameId = Guid.NewGuid();

        try
        {
            File.WriteAllLines(path,
            [
                "gameId,modus,turnIndex,againstBot,candidateCount,evaluatedCandidateCount,onePlyBestScore,onePlySecondBestScore,onePlyScoreGap,reason,twoPlyBestScore,twoPlySecondBestScore,twoPlyScoreGap,bestMoveChanged",
                $"{gameId:D},Backgammon,1,0,5,5,0.5,0.4,0.1,Audit,0.6,0.5,0.1,1"
            ]);

            var decision = Assert.Single(SelectiveTwoPlySearchCsvReader.ReadDecisions(path));
            var summary = SelectiveTwoPlySearchBroker.Analyze([decision]).Overall;

            Assert.Equal(SelectiveTwoPlyReason.Audit, decision.Reason);
            Assert.Null(decision.SelectiveCandidateLimit);
            Assert.Null(decision.TwoPlyBestOnePlyRank);
            Assert.Equal(1, summary.AuditDecisionCount);
            Assert.Equal(1, summary.AuditBestMoveChangedCount);
            Assert.Equal(0, summary.RankedAuditDecisionCount);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void BrokerReportsCoverageAndMoveChanges()
    {
        var report = SelectiveTwoPlySearchBroker.Analyze(CreateDecisions());

        Assert.Equal(4, report.Overall.DecisionCount);
        Assert.Equal(3, report.Overall.EvaluatedDecisionCount);
        Assert.Equal(2, report.Overall.BestMoveChangedCount);
        Assert.Equal(2d / 3d, report.Overall.BestMoveChangeRate, 10);
        Assert.Equal(4d, report.Overall.AverageEvaluatedCandidateCount);
        Assert.Equal(0.205d, report.Overall.MeanOnePlyScoreGap!.Value, 10);
        Assert.Equal(0.35d / 3d, report.Overall.MeanTwoPlyScoreGap!.Value, 10);
        Assert.Equal(2, report.Overall.AuditDecisionCount);
        Assert.Equal(1, report.Overall.AuditBestMoveChangedCount);
        Assert.Equal(0.5d, report.Overall.AuditBestMoveChangeRate);
        Assert.Equal(2, report.Overall.RankedAuditDecisionCount);
        Assert.Equal(1, report.Overall.AuditWinnerOutsideCandidateLimitCount);
        Assert.Equal(0.5d, report.Overall.AuditWinnerOutsideCandidateLimitRate);
        Assert.Equal(1, report.Overall.AuditWinnerOnePlyRankCounts[1]);
        Assert.Equal(1, report.Overall.AuditWinnerOnePlyRankCounts[4]);
        Assert.Equal(1, report.Overall.ReasonCounts[SelectiveTwoPlyReason.Ambiguous]);
        Assert.Equal(1, report.Overall.ReasonCounts[SelectiveTwoPlyReason.GapTooLarge]);
        Assert.Equal(2, report.Overall.ReasonCounts[SelectiveTwoPlyReason.Audit]);
        Assert.Single(report.Groups);
    }

    private static SelectiveTwoPlySearchDecision[] CreateDecisions()
    {
        var gameId = Guid.NewGuid();
        return
        [
            new SelectiveTwoPlySearchDecision(
                gameId,
                GameModus.Backgammon,
                1,
                false,
                5,
                3,
                3,
                0.50d,
                0.49d,
                0.01d,
                SelectiveTwoPlyReason.Ambiguous,
                0.60d,
                0.40d,
                0.20d,
                true,
                2),
            new SelectiveTwoPlySearchDecision(
                gameId,
                GameModus.Backgammon,
                2,
                false,
                4,
                0,
                3,
                0.70d,
                0.59d,
                0.11d,
                SelectiveTwoPlyReason.GapTooLarge,
                null,
                null,
                null,
                null,
                null),
            new SelectiveTwoPlySearchDecision(
                gameId,
                GameModus.Backgammon,
                3,
                false,
                5,
                5,
                3,
                0.80d,
                0.50d,
                0.30d,
                SelectiveTwoPlyReason.Audit,
                0.60d,
                0.50d,
                0.10d,
                true,
                4),
            new SelectiveTwoPlySearchDecision(
                gameId,
                GameModus.Backgammon,
                4,
                false,
                4,
                4,
                3,
                0.90d,
                0.50d,
                0.40d,
                SelectiveTwoPlyReason.Audit,
                0.70d,
                0.65d,
                0.05d,
                false,
                1)
        ];
    }
}